using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Petstore.Kafka
{
    /// <summary>
    /// Concreate factory class for creating a kafka consumer or producer
    /// </summary>
    /// <typeparam name="T">Type of payload to be represented in the consumer/producer</typeparam>
    public class KafkaFactory<T> : IKafkaFactory<T> where T : class
    {
        private readonly ILogger<KafkaFactory<T>> _logger;
        private readonly IOptionsSnapshot<ConsumerConfig> _consumerConfig;
        private readonly IOptionsSnapshot<ProducerConfig> _producerConfig;
        private readonly IOptionsSnapshot<SchemaRegistryConfig> _schemaRegistryConfig;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="consumerConfig">Kafka Consumer Config</param>
        /// <param name="producerConfig">Kafka Producer Config</param>
        /// <param name="schemaRegistryConfig">Schema Registry Config</param>
        public KafkaFactory(ILogger<KafkaFactory<T>> logger, IOptionsSnapshot<ConsumerConfig> consumerConfig, IOptionsSnapshot<ProducerConfig> producerConfig, IOptionsSnapshot<SchemaRegistryConfig> schemaRegistryConfig)
        {
            _logger = logger;
            _consumerConfig = consumerConfig;
            _producerConfig = producerConfig;
            _schemaRegistryConfig = schemaRegistryConfig;
        }

        
        /// <inheritdoc/>
        public IConsumer<string, T> CreateConsumer()
        {
            return new ConsumerBuilder<string, T>(_consumerConfig.Value)
                .SetKeyDeserializer(Deserializers.Utf8)
                .SetErrorHandler((_, e) => _logger.LogError("Kafka Consumer error: {reason}", e.Reason))
                .SetValueDeserializer(new JsonDeserializer<T>().AsSyncOverAsync())
                .Build();
        }

        /// <inheritdoc/>
        public IProducer<string, T> CreateProducer()
        {
            CachedSchemaRegistryClient schemaRegistry = new Confluent.SchemaRegistry.CachedSchemaRegistryClient(_schemaRegistryConfig.Value);
            return new ProducerBuilder<string, T>(_producerConfig.Value)
                .SetKeySerializer(Serializers.Utf8)
                .SetErrorHandler((_, e) => _logger.LogError("Kafka Producer error: {reason}", e.Reason))
                .SetValueSerializer(new JsonSerializer<T>(schemaRegistry, new JsonSerializerConfig()
                {
                    AutoRegisterSchemas = true,
                    SubjectNameStrategy = SubjectNameStrategy.Topic
                }))
                .Build();
        }
    }
}
