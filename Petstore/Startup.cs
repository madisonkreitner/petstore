using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Petstore.EventProcesssors;
using Petstore.Kafka;
using System.Text.Json.Serialization;

namespace InternalOrderformService
{
    /// <summary>
    /// Startup Class
    /// </summary>
    public class Startup
    {
        private readonly string _ServiceTitle = "Petstore";
        private readonly string _ServiceDesc = "Manages pets";
        private readonly WaitHandle _waitHandler = new ManualResetEvent(false);

        /// <summary>
        ///  Constructor
        /// </summary>
        /// <param name="configuration">Configuration</param>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// Configuration
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">Services Collection</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(_waitHandler);

            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });
            services.AddSwaggerDocument(o =>
            {
                o.Title = _ServiceTitle;
                o.Description = _ServiceDesc;
            });

            // configuration
            services
                .Configure<KafkaPetSubmittedConfig>(Configuration.GetSection(nameof(KafkaPetSubmittedConfig)))
                .Configure<ConsumerConfig>(Configuration.GetSection("KafkaConsumerConfig"))
                .Configure<ProducerConfig>(Configuration.GetSection("KafkaProducerConfig"))
                .Configure<SchemaRegistryConfig>(Configuration.GetSection("KafkaSchemaRegistryConfig"));

            services
                .AddHostedService<MainKafkaTopicBkgSvc>()
                .AddScoped<IProcessPetSubmittedEvent, ProcessPetSubmittedEvent>();

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">Application Builder</param>
        /// <param name="env">WebHostEnvironment</param>
        /// <param name="logger">Logger</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Use((context, next) =>
            {
                context.Request.EnableBuffering();
                return next();
            });

            // UNCOMMENT to enable HTTPS redirection
            //app.UseHttpsRedirection();

            app.UseRouting();

            app.UseOpenApi();
            app.UseSwaggerUi3();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            logger.LogDebug("Finished running Startup::Configure()");

            // WARNING: Following MUST be last statement in the function as this will release the background threads to begin processing
            ((ManualResetEvent)_waitHandler).Set();
        }
    }
}