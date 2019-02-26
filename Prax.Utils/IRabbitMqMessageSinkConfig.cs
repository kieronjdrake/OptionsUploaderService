namespace Prax.Utils {
    public interface IRabbitMqMessageSinkConfig {
        string Host { get; }
        int Port { get; }
        string Exchange { get; }
        string RoutingKey { get; }
        string VHost { get; }
        ICredentialsConfig Credentials { get; }
    }
}