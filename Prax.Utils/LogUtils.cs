using NLog;
using NLog.Config;
using NLog.Targets;

namespace Prax.Utils
{
    public static class LogUtils {
        public static MemoryTarget SetupTestLogger(LogLevel minLogLevel)
        {
            // Usage:
            //logger.Info("ow noos");

            ////read the logs here
            //var logs = memoryTarget.Logs;

            var configuration = new LoggingConfiguration();
            var memoryTarget = new MemoryTarget { Name = "mem" };

            configuration.AddTarget(memoryTarget);
            configuration.LoggingRules.Add(new LoggingRule("*", minLogLevel, memoryTarget));
            LogManager.Configuration = configuration;
            return memoryTarget;
        }
    }
}
