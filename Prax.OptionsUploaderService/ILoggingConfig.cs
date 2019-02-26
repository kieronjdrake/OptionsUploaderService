namespace Prax.OptionsUploaderService
{
    public interface ILoggingConfig {
        string LogDirectory { get; }
        string LogstashServer { get; }
        ushort LogstashPort { get; }
    }
}
