using System;
using System.Security.Cryptography;
using System.Text;
using PuzzleGame.Application.Interfaces;
using UnityEngine;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// HMAC-SHA256 save signer. Secret key is derived from device-unique ID +
    /// device model + a baked-in pepper. Tamper detection: a save whose
    /// signature doesn't match the recomputed HMAC is rejected silently.
    /// </summary>
    public sealed class SaveCrypto : ISaveCrypto
    {
        public string SecretKey { get; }

        public SaveCrypto()
        {
            SecretKey = BuildSecretKey();
        }

        private static string BuildSecretKey()
        {
            string deviceId = SystemInfo.deviceUniqueIdentifier ?? "fallback";
            string deviceModel = SystemInfo.deviceModel ?? "unknown";
            const string pepper = "PG-Save-v1-X72kQ9mPr4tFv8wL";
            return $"{deviceId}:{deviceModel}:{pepper}";
        }

        public string GenerateSalt()
        {
            var bytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        public string Sign(string salt, string payload)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SecretKey)))
            {
                byte[] data = Encoding.UTF8.GetBytes(salt + payload);
                byte[] hash = hmac.ComputeHash(data);
                return ByteArrayToHex(hash);
            }
        }

        public bool Verify(string salt, string payload, string expectedHex)
        {
            string actual = Sign(salt, payload);
            return CryptographicEquals(actual, expectedHex);
        }

        private static bool CryptographicEquals(string a, string b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }

        private static string ByteArrayToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
