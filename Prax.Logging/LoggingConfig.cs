using System.Collections.Generic;

namespace Prax.Logging
{
    public enum LoggingType {
        File,
        Elk,
        Console
    }

    public interface ILogConfig {
        LoggingType Type { get; }
        string Target { get; }
    }

    public interface ILoggingConfig {
        IEnumerable<ILogConfig> Logs { get; }
    }
}
