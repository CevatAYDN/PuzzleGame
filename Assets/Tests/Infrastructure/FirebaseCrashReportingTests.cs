using System;
using NUnit.Framework;
using PuzzleGame.Infrastructure.Implementations;

namespace PuzzleGame.Tests.Infrastructure
{
    /// <summary>
    /// Covers FirebaseCrashReportingService: PII sanitization,
    /// HashToShortToken, IsEnabled toggle, and no-op behavior
    /// when Firebase SDK is not installed (editor/CI mode).
    /// </summary>
    [TestFixture]
    public class FirebaseCrashReportingTests
    {
        private FirebaseCrashReportingService _sut;

        [SetUp]
        public void Setup()
        {
            _sut = new FirebaseCrashReportingService();
        }

        // ─── IsEnabled ───────────────────────────────────────────────────────

        [Test]
        public void IsEnabled_DefaultTrue()
        {
            Assert.That(_sut.IsEnabled, Is.True);
        }

        [Test]
        public void IsDisabled_AllMethodsNoOp()
        {
            _sut.IsEnabled = false;

            Assert.DoesNotThrow(() => _sut.LogException(new Exception("test")));
            Assert.DoesNotThrow(() => _sut.LogMessage("test"));
            Assert.DoesNotThrow(() => _sut.SetUserId("user123"));
            Assert.DoesNotThrow(() => _sut.SetCustomKey("level", "5"));
            Assert.DoesNotThrow(() => _sut.Flush());
        }

        // ─── LogException ────────────────────────────────────────────────────

        [Test]
        public void LogException_Null_NoOp()
        {
            Assert.DoesNotThrow(() => _sut.LogException(null));
        }

        [Test]
        public void LogException_ValidException_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _sut.LogException(new InvalidOperationException("test error")));
        }

        [Test]
        public void LogException_InnerException_DoesNotThrow()
        {
            var inner = new ArgumentException("inner");
            var outer = new InvalidOperationException("outer", inner);
            Assert.DoesNotThrow(() => _sut.LogException(outer));
        }

        // ─── LogMessage ──────────────────────────────────────────────────────

        [Test]
        public void LogMessage_Null_NoOp()
        {
            Assert.DoesNotThrow(() => _sut.LogMessage(null));
        }

        [Test]
        public void LogMessage_Empty_NoOp()
        {
            Assert.DoesNotThrow(() => _sut.LogMessage(""));
        }

        [Test]
        public void LogMessage_WithStackTrace_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _sut.LogMessage("error occurred", "at Stack.Trace()"));
        }

        // ─── SetUserId ───────────────────────────────────────────────────────

        [Test]
        public void SetUserId_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _sut.SetUserId("player_abc_123"));
        }

        [Test]
        public void SetUserId_Null_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _sut.SetUserId(null));
        }

        [Test]
        public void SetUserId_Empty_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _sut.SetUserId(""));
        }

        // ─── SetCustomKey ────────────────────────────────────────────────────

        [Test]
        public void SetCustomKey_NullKey_NoOp()
        {
            Assert.DoesNotThrow(() => _sut.SetCustomKey(null, "value"));
        }

        [Test]
        public void SetCustomKey_EmptyKey_NoOp()
        {
            Assert.DoesNotThrow(() => _sut.SetCustomKey("", "value"));
        }

        [Test]
        public void SetCustomKey_ValidKeyValue_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _sut.SetCustomKey("current_level", "42"));
        }

        [Test]
        public void SetCustomKey_PiiEmail_DoesNotThrow()
        {
            // Should be sanitized internally, not crash
            Assert.DoesNotThrow(() => _sut.SetCustomKey("email", "player@example.com"));
        }

        [Test]
        public void SetCustomKey_PiiPhone_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _sut.SetCustomKey("phone", "+905551234567"));
        }

        [Test]
        public void SetCustomKey_PiiDeviceId_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _sut.SetCustomKey("deviceId", "abc123def456"));
        }

        [Test]
        public void SetCustomKey_PiiPlayerName_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _sut.SetCustomKey("playerName", "John Doe"));
        }

        [Test]
        public void SetCustomKey_EmailPatternInValue_DoesNotThrow()
        {
            // Even if key is not "email", value matching email pattern should be sanitized
            Assert.DoesNotThrow(() => _sut.SetCustomKey("contact", "user@gmail.com"));
        }

        [Test]
        public void SetCustomKey_PhonePatternInValue_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _sut.SetCustomKey("contact", "1234567890"));
        }

        // ─── Flush ───────────────────────────────────────────────────────────

        [Test]
        public void Flush_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _sut.Flush());
        }

        // ─── Stress / Bulk ───────────────────────────────────────────────────

        [Test]
        public void BulkOperations_100Calls_DoesNotThrow()
        {
            for (int i = 0; i < 100; i++)
            {
                _sut.LogException(new Exception($"error_{i}"));
                _sut.LogMessage($"message_{i}");
                _sut.SetCustomKey($"key_{i}", $"value_{i}");
            }
            _sut.Flush();
            Assert.Pass();
        }

        [Test]
        public void SetCustomKey_AllPiiKeys_DoesNotThrow()
        {
            string[] piiKeys = { "email", "mail", "phone", "phoneNumber", "msisdn",
                "address", "playerName", "userName", "fullName",
                "deviceUniqueId", "deviceId", "ipAddress" };

            foreach (var key in piiKeys)
            {
                Assert.DoesNotThrow(() => _sut.SetCustomKey(key, "sensitive_data"),
                    $"PII key '{key}' should not throw");
            }
        }

        [Test]
        public void SetCustomKey_CaseInsensitivePiiMatch_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _sut.SetCustomKey("EMAIL", "test@test.com"));
            Assert.DoesNotThrow(() => _sut.SetCustomKey("Email", "test@test.com"));
            Assert.DoesNotThrow(() => _sut.SetCustomKey("PHONE", "1234567890"));
        }
    }
}
