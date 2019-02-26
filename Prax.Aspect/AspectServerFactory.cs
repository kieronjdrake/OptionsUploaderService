using System.Threading;

namespace Prax.Aspect {
    public static class AspectServerFactory {
        public static IServerFacade Create(IAspectConfig config, CancellationToken ct) {
            return new ServerFacade(config, ct);
        }
    }
}