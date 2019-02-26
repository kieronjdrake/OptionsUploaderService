using System;
using Prax.Cryptography;

namespace Prax.AesEncrypt
{
    class Program
    {
        static void Main(string[] args) {
            var parsedArgs = ParseArgs(args);
            if (!parsedArgs.HasValue) {
                ShowUsage();
                return;
            }
            var (keySize, toEncrypt) = parsedArgs.Value;
            var result = keySize == KeySize.OneTwoEight
                             ? Encryption.AesEncryptWith128BitKey(toEncrypt)
                             : Encryption.AesEncryptWith256BitKey(toEncrypt);
            var strings = result.ToStringResult();
            Console.WriteLine("Encrypted:");
            Console.WriteLine(strings.EncrpytedText);
            Console.WriteLine("Key:");
            Console.WriteLine(strings.Key);
            Console.WriteLine("IV:");
            Console.WriteLine(strings.Iv);
        }

        private static (KeySize keySize, string toEncrypt)? ParseArgs(string[] args) {
            if (args.Length < 2) return null;
            switch (args[0]) {
                case "128":
                    return (KeySize.OneTwoEight, args[1]);
                case "256":
                    return (KeySize.TwoFiveSix, args[1]);
                default:
                    return null;
            }
                
        }

        private static void ShowUsage() {
            Console.WriteLine("Usage: Prax.AesEncrypt 128|256 textToEncrypt");
        }

        private enum KeySize {
            OneTwoEight,
            TwoFiveSix
        };
    }
}
