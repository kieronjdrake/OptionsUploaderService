using System;
using Prax.Cryptography;

namespace Prax.Utils
{
    public static class Decrypter {
        public static (string username, string password) DecryptCreds(IEncryptedCredentialsConfig creds) {
            var crypto = new CryptoStringResult(creds.Encrypted, creds.Key, creds.Iv).ToByteArrayResult();
            var decrypted = Encryption.AesDecrypt(crypto);
            var split = decrypted.Split(',');
            if (split.Length != 2) throw new ArgumentException("Encrypted credentials not in valid format");
            return (split[0], split[1]);
        }
    }
}
