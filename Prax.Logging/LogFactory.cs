namespace Prax.Logging
{
    public static class LogFactory {
        public static ILogger CreateLogger() {
            return new ConsoleLogger();
        }
    }
}
