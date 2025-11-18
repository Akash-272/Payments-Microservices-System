using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Text;

namespace WalletService.Messaging
{
    public class RabbitMqProducer : IRabbitMqProducer, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly string _exchange;
        private bool _disposed;

        public RabbitMqProducer(IConfiguration configuration)
        {
            var host = configuration.GetValue<string>("RabbitMq:Host");
            var user = configuration.GetValue<string>("RabbitMq:User");
            var pass = configuration.GetValue<string>("RabbitMq:Password");
            _exchange = configuration.GetValue<string>("RabbitMq:Exchange", "finx.exchange");

            var factory = new ConnectionFactory
            {
                HostName = host,
                UserName = user,
                Password = pass
            };

            // In RabbitMQ 7.x, CreateConnectionAsync is the method to use
            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();

            // CreateChannelAsync is now the async method to get IChannel
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            // ExchangeDeclareAsync is now async
            _channel.ExchangeDeclareAsync(
                exchange: _exchange,
                type: ExchangeType.Topic,
                durable: true
            ).GetAwaiter().GetResult();
        }

        public async Task PublishAsync(string routingKey, string message)
        {
            var body = Encoding.UTF8.GetBytes(message);

            // BasicPublishAsync is now the async method
            await _channel.BasicPublishAsync(
                exchange: _exchange,
                routingKey: routingKey,
                body: body
            );
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _channel?.CloseAsync().GetAwaiter().GetResult();
                _channel?.Dispose();
            }
            catch { }

            try
            {
                _connection?.CloseAsync().GetAwaiter().GetResult();
                _connection?.Dispose();
            }
            catch { }

            _disposed = true;
        }
    }
}