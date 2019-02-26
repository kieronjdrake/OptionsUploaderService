using System;
using Topshelf;
using Topshelf.HostConfigurators;

namespace Prax.Utils {
    public static class ServiceUtils {
        public static void Setup(IServiceConfig config, HostConfigurator hc) {
            SetupServiceRunAs(hc, config);
            SetServiceStart(config.ServiceStart, hc);
            hc.SetDescription(config.ServiceDescription);
            hc.SetDisplayName(config.ServiceDisplayName);
            hc.SetServiceName(config.ServiceName);
        }

        private static void SetupServiceRunAs(HostConfigurator hc, IServiceConfig config) {
            if (config.RunAsLocalSystem) {
                hc.RunAsLocalSystem();
            } else {
                if (config.EncryptedCredentials == null) {
                    throw new ArgumentException("Service set to RunAs but no credentials found");
                }
                var (username, password) = Decrypter.DecryptCreds(config.EncryptedCredentials);
                hc.RunAs(username, password);
            }
        }

        private static void SetServiceStart(ServiceStartType sst, HostConfigurator h) {
            switch (sst) {
                case ServiceStartType.Automatic:
                    h.StartAutomatically();
                    break;
                case ServiceStartType.Delayed:
                    h.StartAutomaticallyDelayed();
                    break;
                case ServiceStartType.Manual:
                    h.StartManually();
                    break;
                case ServiceStartType.Disabled:
                    h.Disabled();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sst), sst, null);
            }
        }
    }
}