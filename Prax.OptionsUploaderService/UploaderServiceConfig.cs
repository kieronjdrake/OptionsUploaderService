using Prax.Utils;

namespace Prax.OptionsUploaderService
{
    public interface IUploaderServiceConfig : IServiceConfig {
        int PollingIntervalSeconds { get; }
    }
}
