using Confluent.Kafka;

namespace Petstore.Kafka
{
    /// <summary>
    /// Factory for create a consumer or producer kafka object
    /// </summary>
    public interface IKafkaFactory<T>
    {
        /// <summary>
        /// Create a Kafka Consumer
        /// </summary>
        /// <returns>Kafka Consumer</returns>
        public IConsumer<string, T> CreateConsumer();

        /// <summary>
        /// Create a Kafka Producer
        /// </summary>
        /// <returns>Kafka Producer</returns>
        public IProducer<string, T> CreateProducer();

    }
}
