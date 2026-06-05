using System;
using NUnit.Framework;
using PuzzleGame.Presentation.UI;

namespace PuzzleGame.Tests.Presentation
{
    [TestFixture]
    public class DailyChallengeCountdownTests
    {
        [Test]
        public void GetNextResetUtc_AtMidnight_ReturnsNextDayMidnight()
        {
            var now = new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc);
            var next = DailyChallengeCountdown.GetNextResetUtc(now);
            Assert.AreEqual(new DateTime(2026, 6, 7, 0, 0, 0, DateTimeKind.Utc), next);
        }

        [Test]
        public void GetNextResetUtc_Afternoon_ReturnsTonightMidnight()
        {
            var now = new DateTime(2026, 6, 6, 15, 30, 45, DateTimeKind.Utc);
            var next = DailyChallengeCountdown.GetNextResetUtc(now);
            Assert.AreEqual(new DateTime(2026, 6, 7, 0, 0, 0, DateTimeKind.Utc), next);
        }

        [Test]
        public void GetNextResetUtc_OneSecondBeforeMidnight_ReturnsMidnight()
        {
            var now = new DateTime(2026, 6, 6, 23, 59, 59, DateTimeKind.Utc);
            var next = DailyChallengeCountdown.GetNextResetUtc(now);
            Assert.AreEqual(new DateTime(2026, 6, 7, 0, 0, 0, DateTimeKind.Utc), next);
        }

        [Test]
        public void GetNextResetUtc_LastDayOfMonth_RollsToNextMonth()
        {
            var now = new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);
            var next = DailyChallengeCountdown.GetNextResetUtc(now);
            Assert.AreEqual(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), next);
        }

        [Test]
        public void GetNextResetUtc_LastDayOfYear_RollsToNextYear()
        {
            var now = new DateTime(2026, 12, 31, 12, 0, 0, DateTimeKind.Utc);
            var next = DailyChallengeCountdown.GetNextResetUtc(now);
            Assert.AreEqual(new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc), next);
        }

        [Test]
        public void GetNextResetUtc_NonUtcInput_Throws()
        {
            var localTime = new DateTime(2026, 6, 6, 12, 0, 0, DateTimeKind.Local);
            Assert.Throws<ArgumentException>(() => DailyChallengeCountdown.GetNextResetUtc(localTime));
            var unspecified = new DateTime(2026, 6, 6, 12, 0, 0, DateTimeKind.Unspecified);
            Assert.Throws<ArgumentException>(() => DailyChallengeCountdown.GetNextResetUtc(unspecified));
        }

        [Test]
        public void GetTimeUntilNextReset_Afternoon_Returns8Hours()
        {
            var now = new DateTime(2026, 6, 6, 16, 0, 0, DateTimeKind.Utc);
            var remaining = DailyChallengeCountdown.GetTimeUntilNextReset(now);
            Assert.AreEqual(8, remaining.TotalHours);
        }

        [Test]
        public void GetTimeUntilNextReset_AtMidnight_Returns24Hours()
        {
            var now = new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc);
            var remaining = DailyChallengeCountdown.GetTimeUntilNextReset(now);
            Assert.AreEqual(24, remaining.TotalHours);
        }

        [Test]
        public void FormatCountdown_OneSecond_FormatsAs00Colon00Colon01()
        {
            var ts = TimeSpan.FromSeconds(1);
            Assert.AreEqual("00:00:01", DailyChallengeCountdown.FormatCountdown(ts));
        }

        [Test]
        public void FormatCountdown_TwoHours45Minutes3Seconds_FormatsCorrectly()
        {
            var ts = new TimeSpan(2, 45, 3);
            Assert.AreEqual("02:45:03", DailyChallengeCountdown.FormatCountdown(ts));
        }

        [Test]
        public void FormatCountdown_25Hours_DisplaysAs25Colon00Colon00()
        {
            var ts = new TimeSpan(25, 0, 0);
            Assert.AreEqual("25:00:00", DailyChallengeCountdown.FormatCountdown(ts));
        }

        [Test]
        public void FormatCountdown_NegativeRemaining_ReturnsZero()
        {
            var ts = TimeSpan.FromSeconds(-5);
            Assert.AreEqual("00:00:00", DailyChallengeCountdown.FormatCountdown(ts));
        }

        [Test]
        public void FormatCountdown_ZeroRemaining_ReturnsZero()
        {
            var ts = TimeSpan.Zero;
            Assert.AreEqual("00:00:00", DailyChallengeCountdown.FormatCountdown(ts));
        }
    }
}
