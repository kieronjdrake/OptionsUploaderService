namespace Prax.Logging
{
    public interface ILogger {
        void Log(LogLevel level, string message);
    }
}
