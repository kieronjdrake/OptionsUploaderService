namespace Prax.Utils {
    public interface IEmailMessageSinkConfig {
        string Host { get; }
        string From { get; }
        string To { get; }
        string Subject { get; }
    }
}
