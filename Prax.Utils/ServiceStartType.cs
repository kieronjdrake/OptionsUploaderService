using System;
using Topshelf;
using Topshelf.HostConfigurators;

namespace Prax.Utils
{
    public enum ServiceStartType {
        Automatic,
        Delayed,
        Manual,
        Disabled
    }
}
