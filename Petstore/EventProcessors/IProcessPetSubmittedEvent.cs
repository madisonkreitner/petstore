namespace Petstore.EventProcesssors
{
    /// <summary>
    /// IProcessPetSubmittedEvent
    /// </summary>
    public interface IProcessPetSubmittedEvent
    {
        /// <summary>
        /// Listen for PetSubmittedEvent on the main topic and process them
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        Task ListenAndProcessEventsAsync(CancellationToken stoppingToken);
    }
}