using System;
using System.Text;
using RabbitMQ.Client;

namespace Prax.Utils
{
    public class RabbitMqMessageSink : IMessageSink
    {
        private readonly IConnection _connection;
        private IModel _channel;
        private readonly string _exchange;
        private readonly string _routingKey;

        public RabbitMqMessageSink(IRabbitMqMessageSinkConfig config) {
            var (username, password) = config.Credentials.GetCredentials();
            _exchange = config.Exchange;
            _routingKey = config.RoutingKey;
            var factory = new ConnectionFactory {
                Uri = new Uri(config.Host),
                Port = config.Port,
                UserName = username,
                Password = password,
                VirtualHost = config.VHost
            };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
        }

        // For RabbitMQ messages we publish for all Aspect instances, primary or not
        public void SendMessage(string message, bool isPrimaryAspectInstance) {
            // From https://www.rabbitmq.com/dotnet-api-guide.html
            // "since many recoverable protocol errors will result in channel closure, channel lifespan could be
            //  shorter than that of its connection"
            if (_channel.IsClosed) {
                _channel = _connection.CreateModel();
            }
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var props = _channel.CreateBasicProperties();
            props.Persistent = true;
            _channel.BasicPublish(_exchange, _routingKey, mandatory: false, basicProperties: props, body: messageBytes);
        }

        public void Close() {
            _connection.Close();
        }
    }
}
