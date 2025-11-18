namespace TransactionService.Messaging
{
    public interface IRabbitMqConsumer
    {
        void StartConsuming();
        void StopConsuming();
    }
}
