using System;

namespace Prax.Aspect {
    public enum AspectEnvironment {
        Test,
        Staging,
        Production
    }

    public static class AspectEnvironmentExtensions {
        public static string GetServiceUrl(this AspectEnvironment environment) {
            switch (environment) {
                case AspectEnvironment.Test:
                    return AspectTestUrl;
                case AspectEnvironment.Staging:
                    return AspectStagingUrl;
                case AspectEnvironment.Production:
                    return AspectLiveUrl;
                default:
                    throw new ArgumentOutOfRangeException(nameof(environment), environment, null);
            }
        }

        private const string AspectTestUrl = "https://test.myaspect.net/webservice/aspectws";
        private const string AspectStagingUrl = "https://myaspect.net/webservice/aspectex";
        private const string AspectLiveUrl = "https://myaspect.net/webservice/aspectws";
    }
}