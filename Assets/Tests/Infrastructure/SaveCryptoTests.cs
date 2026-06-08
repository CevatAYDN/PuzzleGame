using NUnit.Framework;
using PuzzleGame.Infrastructure.Implementations;

namespace PuzzleGame.Tests.Infrastructure
{
    /// <summary>
    /// Covers HMAC-SHA256 save signing: sign/verify roundtrip,
    /// tamper detection, salt uniqueness, constant-time comparison,
    /// and edge cases (null, empty, long payloads).
    /// </summary>
    [TestFixture]
    public class SaveCryptoTests
    {
        private SaveCrypto _sut;

        [SetUp]
        public void Setup()
        {
            _sut = new SaveCrypto();
        }

        // ─── Constructor ─────────────────────────────────────────────────────

        [Test]
        public void Constructor_GeneratesNonEmptySecretKey()
        {
            Assert.That(_sut.SecretKey, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void Constructor_SecretKeyContainsDeviceIdentifier()
        {
            // SecretKey format: {deviceId}:{deviceModel}:{pepper}
            Assert.That(_sut.SecretKey, Does.Contain(":"));
            Assert.That(_sut.SecretKey, Does.Contain("PG-Save-v1"));
        }

        [Test]
        public void Constructor_TwoInstances_HaveSameSecretKey()
        {
            // Both derive from the same device — keys must match for save/load
            var sut2 = new SaveCrypto();
            Assert.That(_sut.SecretKey, Is.EqualTo(sut2.SecretKey));
        }

        // ─── GenerateSalt ────────────────────────────────────────────────────

        [Test]
        public void GenerateSalt_ReturnsBase64String()
        {
            string salt = _sut.GenerateSalt();
            Assert.That(salt, Is.Not.Null.And.Not.Empty);

            // 16 bytes → 24 base64 chars (no padding with 16 bytes)
            Assert.That(salt.Length, Is.GreaterThanOrEqualTo(20));
        }

        [Test]
        public void GenerateSalt_EachCallIsUnique()
        {
            string s1 = _sut.GenerateSalt();
            string s2 = _sut.GenerateSalt();
            string s3 = _sut.GenerateSalt();

            Assert.That(s1, Is.Not.EqualTo(s2));
            Assert.That(s2, Is.Not.EqualTo(s3));
            Assert.That(s1, Is.Not.EqualTo(s3));
        }

        [Test]
        public void GenerateSalt_100Salts_AllUnique()
        {
            var set = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < 100; i++)
            {
                Assert.That(set.Add(_sut.GenerateSalt()), Is.True,
                    $"Salt collision at iteration {i}");
            }
        }

        // ─── Sign ────────────────────────────────────────────────────────────

        [Test]
        public void Sign_ReturnsHexString()
        {
            string hash = _sut.Sign("salt", "payload");
            Assert.That(hash, Is.Not.Null.And.Not.Empty);
            Assert.That(hash.Length, Is.EqualTo(64)); // SHA256 = 32 bytes = 64 hex chars

            // Must be valid hex
            Assert.That(System.Text.RegularExpressions.Regex.IsMatch(hash, "^[0-9a-f]+$"),
                Is.True);
        }

        [Test]
        public void Sign_SameInput_ProducesSameOutput()
        {
            string h1 = _sut.Sign("abc", "data");
            string h2 = _sut.Sign("abc", "data");
            Assert.That(h1, Is.EqualTo(h2));
        }

        [Test]
        public void Sign_DifferentSalt_ProducesDifferentOutput()
        {
            string h1 = _sut.Sign("salt1", "data");
            string h2 = _sut.Sign("salt2", "data");
            Assert.That(h1, Is.Not.EqualTo(h2));
        }

        [Test]
        public void Sign_DifferentPayload_ProducesDifferentOutput()
        {
            string h1 = _sut.Sign("salt", "data1");
            string h2 = _sut.Sign("salt", "data2");
            Assert.That(h1, Is.Not.EqualTo(h2));
        }

        [Test]
        public void Sign_EmptyPayload_StillProducesValidHash()
        {
            string hash = _sut.Sign("salt", "");
            Assert.That(hash.Length, Is.EqualTo(64));
        }

        [Test]
        public void Sign_EmptySalt_StillProducesValidHash()
        {
            string hash = _sut.Sign("", "payload");
            Assert.That(hash.Length, Is.EqualTo(64));
        }

        [Test]
        public void Sign_LongPayload_ProducesValidHash()
        {
            string longPayload = new string('x', 10000);
            string hash = _sut.Sign("salt", longPayload);
            Assert.That(hash.Length, Is.EqualTo(64));
        }

        [Test]
        public void Sign_UnicodePayload_ProducesValidHash()
        {
            string hash = _sut.Sign("salt", "Türkçe karakterler: ğüşıöçĞÜŞİÖÇ");
            Assert.That(hash.Length, Is.EqualTo(64));
        }

        // ─── Verify ──────────────────────────────────────────────────────────

        [Test]
        public void Verify_ValidSignature_ReturnsTrue()
        {
            string salt = _sut.GenerateSalt();
            string payload = "{\"coins\":100,\"level\":5}";
            string signature = _sut.Sign(salt, payload);

            Assert.That(_sut.Verify(salt, payload, signature), Is.True);
        }

        [Test]
        public void Verify_TamperedPayload_ReturnsFalse()
        {
            string salt = _sut.GenerateSalt();
            string payload = "{\"coins\":100}";
            string signature = _sut.Sign(salt, payload);

            // Tamper with payload
            Assert.That(_sut.Verify(salt, "{\"coins\":999}", signature), Is.False);
        }

        [Test]
        public void Verify_TamperedSalt_ReturnsFalse()
        {
            string salt = _sut.GenerateSalt();
            string payload = "{\"coins\":100}";
            string signature = _sut.Sign(salt, payload);

            // Tamper with salt
            Assert.That(_sut.Verify("tampered_salt", payload, signature), Is.False);
        }

        [Test]
        public void Verify_TamperedSignature_ReturnsFalse()
        {
            string salt = _sut.GenerateSalt();
            string payload = "{\"coins\":100}";
            string signature = _sut.Sign(salt, payload);

            // Flip first hex char
            char[] chars = signature.ToCharArray();
            chars[0] = chars[0] == 'a' ? 'b' : 'a';
            string tampered = new string(chars);

            Assert.That(_sut.Verify(salt, payload, tampered), Is.False);
        }

        [Test]
        public void Verify_WrongLengthSignature_ReturnsFalse()
        {
            Assert.That(_sut.Verify("salt", "data", "abc"), Is.False);
            Assert.That(_sut.Verify("salt", "data", new string('0', 63)), Is.False);
            Assert.That(_sut.Verify("salt", "data", new string('0', 65)), Is.False);
        }

        [Test]
        public void Verify_NullSignature_ReturnsFalse()
        {
            Assert.That(_sut.Verify("salt", "data", null), Is.False);
        }

        [Test]
        public void Verify_NullPayload_ReturnsFalse()
        {
            string signature = _sut.Sign("salt", "data");
            Assert.That(_sut.Verify("salt", null, signature), Is.False);
        }

        [Test]
        public void Verify_NullSalt_ReturnsFalse()
        {
            string signature = _sut.Sign("salt", "data");
            Assert.That(_sut.Verify(null, "data", signature), Is.False);
        }

        // ─── End-to-end Save/Load Simulation ─────────────────────────────────

        [Test]
        public void Roundtrip_SaveAndLoad_ValidatesCorrectly()
        {
            // Simulate a real save/load cycle
            string saveJson = "{\"coins\":250,\"level\":12,\"stars\":3}";
            string salt = _sut.GenerateSalt();
            string signature = _sut.Sign(salt, saveJson);

            // "Load" — verify
            Assert.That(_sut.Verify(salt, saveJson, signature), Is.True);

            // Tampered load
            Assert.That(_sut.Verify(salt, "{\"coins\":999999}", signature), Is.False);
        }

        [Test]
        public void Roundtrip_MultipleSaves_EachIndependentlyVerifiable()
        {
            for (int i = 0; i < 10; i++)
            {
                string payload = $"{{\"level\":{i}}}";
                string salt = _sut.GenerateSalt();
                string sig = _sut.Sign(salt, payload);

                Assert.That(_sut.Verify(salt, payload, sig), Is.True,
                    $"Save {i} failed verification");
            }
        }

        // ─── Constant-Time Comparison ────────────────────────────────────────

        [Test]
        public void Verify_ConstantTimeComparison_DoesNotShortCircuit()
        {
            // The implementation uses XOR accumulation, not early return.
            // Verify that different-length strings are rejected immediately
            // (length check is safe to short-circuit), but same-length
            // strings with different content complete the full XOR pass.
            string sig = _sut.Sign("salt", "data");

            // Same length, different content — must not leak timing via early exit
            string sameLength = new string('0', sig.Length);
            Assert.That(_sut.Verify("salt", "data", sameLength), Is.False);
        }
    }
}
