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
    /// <remarks>
    /// SECURITY MODEL — what this protects against and what it does NOT:
    /// <list type="bullet">
    /// <item><b>Protects against</b>: a casual player opening the save file in a
    ///   text editor, swapping the JSON, and expecting the game to accept it.
    ///   Without a valid HMAC the new payload is rejected.</item>
    /// <item><b>Does NOT protect against</b>: a determined attacker with the
    ///   binary. The pepper is compiled into the DLL and recoverable via
    ///   reflection or runtime memory inspection. On a rooted Android device
    ///   or jailbroken iPhone the attacker can also call the same <see cref="Sign"/>
    ///   path with the captured device identifier and forge a valid signature.</item>
    /// </list>
    /// <para>
    /// For real protection of paid content / IAP unlocks:
    /// </para>
    /// <list type="number">
    /// <item>Store the per-user HMAC key in <c>Android Keystore</c>
    ///   (<c>KeyStore.getInstance("AndroidKeyStore")</c>) or <c>iOS Keychain</c>
    ///   (<c>kSecAttrAccessibleAfterFirstUnlock</c>). These are isolated by the
    ///   OS and survive a binary swap.</item>
    /// <item>Validate receipts server-side (see <c>PurchaseController</c>).</item>
    /// </list>
    /// <para>
    /// Treat this signer as defence-in-depth — it raises the bar, it does not
    /// close the door.
    /// </para>
    /// </remarks>
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
            // Baked-in pepper. NOT a secret in the cryptographic sense — it is
            // shipped in the binary. See class remarks for the security model.
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
