namespace WalletService.Messaging
{
    public interface IRabbitMqProducer
    {
        Task PublishAsync(string routingKey, string message);
    }
}
