using System.Collections.Generic;
using Prax.Aspect;

namespace Prax.OptionsUploaderService
{
    public interface IAspectEndpointsConfig {
        IEnumerable<IAspectConfig> Servers { get; }
    }
}
