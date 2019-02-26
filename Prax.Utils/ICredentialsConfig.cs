using System;

namespace Prax.Utils
{
    /// <summary>
    /// Node to hold either plain text or encrypted credentials
    /// </summary>
    public interface ICredentialsConfig {
        IEncryptedCredentialsConfig EncryptedCredentials { get; }
        IPlainTextCredentialsConfig PlainTextCredentials { get; }
    }

    /// <summary>
    /// AES encrypted credentials in the format username,password
    /// Run something like Prax.AesEncrypt to generate the encrypted text, key and IV / nonce
    /// </summary>
    public interface IEncryptedCredentialsConfig {
        string Encrypted { get; }
        string Key { get; }
        string Iv { get; }
    }

    /// <summary>
    /// Plain text credentials, for use when developing / debugging. Should not be used in production.
    /// </summary>
    public interface IPlainTextCredentialsConfig {
        string Username { get; }
        string Password { get; }
    }

    public static class CredentialsConfigExtensions {
        public static (string username, string password) GetCredentials(this ICredentialsConfig config) {
            if (config.PlainTextCredentials != null) {
                return (config.PlainTextCredentials.Username, config.PlainTextCredentials.Password);
            } 
            if (config.EncryptedCredentials != null) {
                return Decrypter.DecryptCreds(config.EncryptedCredentials);
            }
            throw new ArgumentException("Invalid credentials configuration");
        }
    }
}
