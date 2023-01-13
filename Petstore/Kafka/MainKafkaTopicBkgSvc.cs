using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Threading;
using System;
using Petstore.EventProcesssors;

namespace Petstore.Kafka
{
	/// <summary>
	/// This service is used to create scoping around the actual service doing work such that the scoped service can take advantage of the built-in DI of .NET Core
	/// </summary>
	public class MainKafkaTopicBkgSvc : BackgroundService
	{
		#region Fields
		private readonly ILogger<MainKafkaTopicBkgSvc> _logger;
		private readonly IServiceProvider _services;
        private readonly WaitHandle _waitHandle;
		#endregion

		#region Constructors
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="logger">Logger</param>
		/// <param name="services">Services Provider</param>
		/// <param name="waitHandle">Signals when the job can begin performing work</param>
		public MainKafkaTopicBkgSvc(ILogger<MainKafkaTopicBkgSvc> logger, IServiceProvider services, WaitHandle waitHandle)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_services = services ?? throw new ArgumentNullException(nameof(services));
			_waitHandle = waitHandle ?? throw new ArgumentNullException(nameof(waitHandle));
		}
		#endregion

		#region Methods
		/// <summary>
		/// Executes the background task
		/// </summary>
		/// <param name="stoppingToken">Concellation token</param>
		/// <returns></returns>
		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			// This yield is needed to make the background service completely async to the rest of the WebApi setup process
			await Task.Yield();

			// Let the startup for the app finish before we begin processing anything
			WaitHandle.WaitAny(new WaitHandle[] { _waitHandle });

			_logger.LogInformation("{service} started at: {time}", nameof(MainKafkaTopicBkgSvc), DateTimeOffset.UtcNow);
			try
            {
				while (!stoppingToken.IsCancellationRequested)
				{
					// Chain cancellation toeksn to sub services in order to stop them if the parent service is requested to stop.
					CancellationToken subTaskStoppingToken = GetScopedCancellationToken(stoppingToken);

					// Create a scope to make DI available to the service that will be performing all the work
					using IServiceScope? scope = _services.CreateScope();

					// Get the service that is going to be doing all the work.
					IProcessPetSubmittedEvent? processingService = scope.ServiceProvider.GetRequiredService<IProcessPetSubmittedEvent>();

					// Call the fetched service to begin performing the work.
					_logger.LogDebug("Calling the ListenAndProcessEventsAsync() on the service: {service}", processingService.GetType().Name);
					await processingService.ListenAndProcessEventsAsync(subTaskStoppingToken).ConfigureAwait(false);
				}
			}
			catch (Exception e)
            {
				_logger.LogError(e, "Problem executing task.");
				throw;
            }
			finally
            {
				_logger.LogInformation("{service} stopped at: {time}", nameof(MainKafkaTopicBkgSvc), DateTimeOffset.UtcNow);
			}
		}

		/// <summary>
		/// Creates a new CancellationToken for managing sub tasks tied to parent CancellationToken.
		/// </summary>
		/// <param name="originalToken">the parent CancellationToken</param>
		/// <param name="existingSource">an existing CancellationTokenSource</param>
		/// <returns>A new CancellationToken that will be cncelled when the original is cancelled.</returns>
		public static CancellationToken GetScopedCancellationToken(CancellationToken originalToken, CancellationTokenSource? existingSource = null)
		{
            CancellationTokenSource source = existingSource ?? new CancellationTokenSource();
			// Intentionally passing the new source.Token to the Task.Run,
			// as this task will be canceled before the source can be canceled if we pass the originalToken.
			Task.Run(() =>
			{
				do
				{
					if (originalToken.IsCancellationRequested)
					{
						source.Cancel();
					}
				}
				while (!originalToken.IsCancellationRequested && !source.Token.IsCancellationRequested);
			}, source.Token);
			return source.Token;
		}
		#endregion
	}
}
