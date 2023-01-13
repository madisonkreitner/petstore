using Confluent.Kafka;
using IO.Swagger.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Petstore.Kafka;
using System;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Petstore.EventProcesssors
{
    /// <summary>
    /// Represents a service object that is going to do work
    /// </summary>
    public class ProcessPetSubmittedEvent : IProcessPetSubmittedEvent
    {
        #region Fields
        private const string cRetryCount = "RetryCount";
        private const string cRedirectTopic = "RedirectTopic";
        private const string cGroupId = "GroupId";
        private readonly ILogger<ProcessPetSubmittedEvent> _logger;
        private readonly IOptionsSnapshot<KafkaPetSubmittedConfig> _kafkaPetSubmittedConfig;
        private readonly IKafkaFactory<Pet> _kafkaFactory;
        private readonly IOptionsSnapshot<ConsumerConfig> _consumerConfig;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <remarks>
        ///   Additional objects can be passed into the constructor using the built-in DI mechanism because 
        ///   this service is automatically scoped due to the ConsumeServiceAsBackgroundWorker object
        ///   see https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-6.0&amp;tabs=visual-studio#consuming-a-scoped-service-in-a-background-task
        /// </remarks>
        /// <param name="logger">Logger</param>
        /// <param name="kafkaInternalProductChangedConfig">Configuration for OrderForm Submitted related topics</param>
        /// <param name="kafkaFactory">Kafka Factory for creating producer/consumers</param>
        /// <param name="serviceHealthCheckin">Health checkin</param>
        /// <param name="productMapContext">EF for Product Changeds</param>
        /// <param name="consumerConfig">Kafka Consumer Configuration</param>
        /// <param name="kafkaMessageService">Kafka Message Service</param>
        public ProcessPetSubmittedEvent(ILogger<ProcessPetSubmittedEvent> logger,
                                             IOptionsSnapshot<KafkaPetSubmittedConfig> kafkaInternalProductChangedConfig,
                                             IKafkaFactory<Pet> kafkaFactory,
                                             IOptionsSnapshot<ConsumerConfig> consumerConfig)
        {
            _logger = logger;
            _kafkaPetSubmittedConfig = kafkaInternalProductChangedConfig;
            _kafkaFactory = kafkaFactory;
            _consumerConfig = consumerConfig;
        }
        #endregion

        #region Topic Processors
        /// <summary>
        /// Performs the actual work on the main topic
        /// </summary>
        /// <param name="stoppingToken">Cancellation token</param>
        /// <returns>Task</returns>
        public async Task ListenAndProcessEventsAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{service} Main processor started at: {time}", nameof(ProcessPetSubmittedEvent), DateTimeOffset.UtcNow);

            await AddATestMessage();

            using (IConsumer<string, Pet> consumer = _kafkaFactory.CreateConsumer())
            {
                consumer.Subscribe(_kafkaPetSubmittedConfig.Value.TopicName);
                try
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            ConsumeResult<string, Pet> msg = consumer.Consume(5000);
                            if (msg?.Message != null)
                            {
                                if (await ProcessTheMessageAsync(msg, stoppingToken).ConfigureAwait(false))
                                {
                                    _logger.LogInformation("Finished initial processing of pet {pet}.", msg.Message.Value?.Name);
                                }
                                else
                                {
                                    _logger.LogWarning("Unable to successfully process add pet event for {name}.  Will retry later.", msg.Message.Value?.Name);

                                    // Place the message on the retry topic
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            // Attempt to consume all messages, do not throw.
                            _logger.LogError(e, "Problem running background service task. Exiting task.");
                            throw;
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Exception while listening and processing Product Changed events.");
                    throw;
                }
                finally
                {
                    consumer.Close();
                    _logger.LogInformation("{service} Main processor stopped at: {time}", nameof(ProcessPetSubmittedEvent), DateTimeOffset.UtcNow);
                }
            }
        }
        #endregion

        #region Private
        /// <summary>
        /// Processes the individual message
        /// </summary>
        /// <param name="msg">Kafka Message to process</param>
        /// <param name="stoppingToken">Cancellation token</param>
        /// <returns>TRUE if the message was processed successfully, FALSE otherwise</returns>
        private async Task<bool> ProcessTheMessageAsync(ConsumeResult<string, Pet> msg, CancellationToken stoppingToken)
        {
            _logger.LogDebug("About to process the Product Changed message {@msg}", msg);
            try
            {
                Pet received = msg.Message.Value;
                // check for required values, edge cases, etc 

                // do something with the pet submitted
                await Task.Delay(1000);
                _logger.LogDebug("Did something with pet {@pet}", received);

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Problem trying to process the Pet Submitted message {@message}", msg.Message);
                // Return false so that we can put it into the retry queue
                return false;
            }
        }

        private async Task AddATestMessage()
        {
            IProducer<string, Pet> producer = _kafkaFactory.CreateProducer();
            Message<string, Pet> prodMsg = new Message<string, Pet>()
            {
                Key = "123456",
                Value = new Pet()
                {
                    Category = new Category() { Id = 1, Name= "test" },
                    Id= 1,
                    PhotoUrls = new() { "/path/to/file/new" },
                    Name= "test",
                    Status = Pet.StatusEnum.AvailableEnum,
                    Tags = new List<Tag>()
                }
            };

            try
            {
                await producer.ProduceAsync(_kafkaPetSubmittedConfig.Value.TopicName, prodMsg);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Publish issue");
            }
        }
        #endregion
    }
}
