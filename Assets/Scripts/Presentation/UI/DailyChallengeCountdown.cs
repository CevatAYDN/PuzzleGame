using System;

namespace PuzzleGame.Presentation.UI
{
    /// <summary>
    /// Daily challenge resets at UTC midnight (GDD-mandated, deterministic across timezones).
    /// Pure POCO — testable, Unity-agnostic. Used by DailyChallengeController to render countdown.
    /// </summary>
    public static class DailyChallengeCountdown
    {
        public const int ResetHourUtc = 0;

        public static DateTime GetNextResetUtc(DateTime nowUtc)
        {
            if (nowUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("nowUtc must be UTC", nameof(nowUtc));
            return new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(1);
        }

        public static DateTime GetNextResetUtc()
        {
            return GetNextResetUtc(DateTime.UtcNow);
        }

        public static TimeSpan GetTimeUntilNextReset(DateTime nowUtc)
        {
            return GetNextResetUtc(nowUtc) - nowUtc;
        }

        public static TimeSpan GetTimeUntilNextReset()
        {
            return GetTimeUntilNextReset(DateTime.UtcNow);
        }

        public static string FormatCountdown(TimeSpan remaining)
        {
            if (remaining.TotalSeconds < 0) remaining = TimeSpan.Zero;
            int hours = (int)remaining.TotalHours;
            return string.Format("{0:D2}:{1:D2}:{2:D2}", hours, remaining.Minutes, remaining.Seconds);
        }
    }
}
