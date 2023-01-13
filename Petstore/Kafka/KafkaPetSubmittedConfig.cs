namespace Petstore.Kafka
{
    public class KafkaPetSubmittedConfig
    {
        /// <summary>
        /// Name of the topic that contains the ProductChanged events
        /// </summary>
        public string TopicName { get; set; } = "pet_submitted_event";

        /// <summary>
        /// Name of the topic that allows for retrying of the messages.
        /// </summary>
        public string RetryTopicName { get; set; } = "pet_submitted_event_retry";

        /// <summary>
        /// Name of the topic that represents the deadletter area for messages that failed to be processed after multiple attempts
        /// </summary>
        public string DeadletterTopicName { get; set; } = "deadletter_event";

        /// <summary>
        /// Maximum number of times to retry processing the message before placing it on the deadletter topic
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 5;
    }
}
