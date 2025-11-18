using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using TransactionService.Services;

namespace TransactionService.Messaging
{
    public class RabbitMqConsumer : IRabbitMqConsumer, IDisposable
    {
        private readonly IConfiguration _config;
        private readonly IConnection? _connection;
        private readonly IChannel? _channel;
        private readonly ITransactionService _txService;
        private readonly ILogger<RabbitMqConsumer> _logger;
        private readonly string _exchange;
        private readonly string? _queueName;
        private readonly bool _isConnected;
        private bool _disposed;

        public RabbitMqConsumer(
            IConfiguration configuration,
            ITransactionService txService,
            ILogger<RabbitMqConsumer> logger)
        {
            _config = configuration;
            _txService = txService;
            _logger = logger;
            _exchange = configuration.GetValue<string>("RabbitMq:Exchange", "finx.exchange");

            try
            {
                var host = configuration.GetValue<string>("RabbitMq:Host");
                var user = configuration.GetValue<string>("RabbitMq:User");
                var pass = configuration.GetValue<string>("RabbitMq:Password");

                var factory = new ConnectionFactory
                {
                    HostName = host,
                    UserName = user,
                    Password = pass,
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(5)
                };

                _logger.LogInformation("Attempting to connect to RabbitMQ at {Host}...", host);

                // Use async methods for RabbitMQ 7.x
                _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
                _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

                // Declare exchange
                _channel.ExchangeDeclareAsync(
                    exchange: _exchange,
                    type: ExchangeType.Topic,
                    durable: true
                ).GetAwaiter().GetResult();

                // Declare queue
                var queueDeclareResult = _channel.QueueDeclareAsync(
                    queue: "", // Auto-generated name
                    durable: false,
                    exclusive: true,
                    autoDelete: true
                ).GetAwaiter().GetResult();

                _queueName = queueDeclareResult.QueueName;

                // Bind queue to routing keys
                _channel.QueueBindAsync(_queueName, _exchange, "wallet.credited").GetAwaiter().GetResult();
                _channel.QueueBindAsync(_queueName, _exchange, "wallet.debited").GetAwaiter().GetResult();
                _channel.QueueBindAsync(_queueName, _exchange, "wallet.transferred").GetAwaiter().GetResult();

                _isConnected = true;
                _logger.LogInformation("Successfully connected to RabbitMQ and bound to queue: {QueueName}", _queueName);
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _logger.LogWarning(ex,
                    "Failed to connect to RabbitMQ. Consumer will not process messages. " +
                    "Please ensure RabbitMQ is running on localhost:5672");
            }
        }

        public void StartConsuming()
        {
            if (!_isConnected || _channel == null || string.IsNullOrEmpty(_queueName))
            {
                _logger.LogWarning("Cannot start consuming: RabbitMQ is not connected");
                return;
            }

            try
            {
                var consumer = new AsyncEventingBasicConsumer(_channel);

                consumer.ReceivedAsync += async (model, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);

                        _logger.LogInformation("Received message: {Message}", message);

                        // Parse message JSON
                        var doc = JsonDocument.Parse(message);
                        if (!doc.RootElement.TryGetProperty("Event", out var ev))
                        {
                            _logger.LogWarning("Message missing 'Event' property");
                            await _channel.BasicAckAsync(ea.DeliveryTag, false);
                            return;
                        }

                        var eventName = ev.GetString();
                        switch (eventName)
                        {
                            case "WalletCredited":
                                {
                                    var walletId = doc.RootElement.GetProperty("WalletId").GetGuid().ToString();
                                    var userId = doc.RootElement.GetProperty("UserId").GetString()!;
                                    var amount = doc.RootElement.GetProperty("Amount").GetDecimal();
                                    var reference = doc.RootElement.TryGetProperty("Reference", out var r) ? r.GetString() : null;

                                    await _txService.CreateLedgerEntryAsync(userId, walletId, "CREDIT", amount, reference);
                                    _logger.LogInformation("Processed WalletCredited for user {UserId}", userId);
                                    break;
                                }
                            case "WalletDebited":
                                {
                                    var walletId = doc.RootElement.GetProperty("WalletId").GetGuid().ToString();
                                    var userId = doc.RootElement.GetProperty("UserId").GetString()!;
                                    var amount = doc.RootElement.GetProperty("Amount").GetDecimal();
                                    var reference = doc.RootElement.TryGetProperty("Reference", out var r) ? r.GetString() : null;

                                    await _txService.CreateLedgerEntryAsync(userId, walletId, "DEBIT", amount, reference);
                                    _logger.LogInformation("Processed WalletDebited for user {UserId}", userId);
                                    break;
                                }
                            case "WalletTransferred":
                                {
                                    var fromUser = doc.RootElement.GetProperty("FromUserId").GetString()!;
                                    var toUser = doc.RootElement.GetProperty("ToUserId").GetString()!;
                                    var amount = doc.RootElement.GetProperty("Amount").GetDecimal();
                                    var reference = doc.RootElement.TryGetProperty("Reference", out var r) ? r.GetString() : null;

                                    // Create two ledger entries for transfer
                                    await _txService.CreateLedgerEntryAsync(fromUser, fromUser, "TRANSFER_OUT", amount, reference);
                                    await _txService.CreateLedgerEntryAsync(toUser, toUser, "TRANSFER_IN", amount, reference);
                                    _logger.LogInformation("Processed WalletTransferred from {FromUser} to {ToUser}", fromUser, toUser);
                                    break;
                                }
                            default:
                                _logger.LogWarning("Unknown event type: {EventName}", eventName);
                                break;
                        }

                        // Acknowledge message
                        await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message");
                        // Negative acknowledge and requeue
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                    }
                };

                // Start consuming with async consumer
                _channel.BasicConsumeAsync(
                    queue: _queueName,
                    autoAck: false,
                    consumer: consumer
                ).GetAwaiter().GetResult();

                _logger.LogInformation("Started consuming messages from queue: {QueueName}", _queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting consumer");
            }
        }

        public void StopConsuming()
        {
            if (_channel != null)
            {
                try
                {
                    _channel.CloseAsync().GetAwaiter().GetResult();
                    _logger.LogInformation("RabbitMQ channel closed");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing channel");
                }
            }

            if (_connection != null)
            {
                try
                {
                    _connection.CloseAsync().GetAwaiter().GetResult();
                    _logger.LogInformation("RabbitMQ connection closed");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing connection");
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            StopConsuming();

            try { _channel?.Dispose(); } catch { }
            try { _connection?.Dispose(); } catch { }

            _disposed = true;
        }
    }
}