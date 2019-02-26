using NLog;
using Prax.Utils;

namespace Prax.Aspect
{
    public enum MaxLogLevel {
        Trace,
        Debug,
        Info,
        Warn,
        Error,
        Fatal
    }

    public interface IAspectConfig {
        AspectEnvironment AspectEnvironment { get; }
        int WebServiceTimeoutMinutes { get; }
        int ConnectionFailureRetryCount { get; }
        ICredentialsConfig Credentials { get; }
        bool IsPrimary { get; }
        MaxLogLevel? MaxLogLevel { get; }
        UploadMethod UploadMethod { get; }
    }
}
