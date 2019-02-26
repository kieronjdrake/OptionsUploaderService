namespace Prax.Utils {
    /// <summary>
    /// This represents the basic settings for running as a windows service
    /// </summary>
    public interface IServiceConfig {
        bool RunAsLocalSystem { get; }
        IEncryptedCredentialsConfig EncryptedCredentials { get; }
        ServiceStartType ServiceStart { get; }
        string ServiceDescription { get; }
        string ServiceDisplayName { get; }
        string ServiceName { get; }
    }
}