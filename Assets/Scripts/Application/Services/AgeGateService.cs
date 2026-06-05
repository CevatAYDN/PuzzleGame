using System;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using UnityEngine;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// COPPA age gate. Persists birth year + month (no PII day) via PlayerPrefs
    /// and exposes IsUnder13 for consent gating and ads disabling.
    /// </summary>
    public class AgeGateService : IAgeVerificationService
    {
        private const string LogTag = "[AgeGate]";
        private const string SaveKeyBirthYear = "agegate_birth_year";
        private const string SaveKeyBirthMonth = "agegate_birth_month";
        private const int CoppaAgeThreshold = 13;

        public bool IsVerified => BirthDate.HasValue;

        public bool IsUnder13
        {
            get
            {
                if (!BirthDate.HasValue) return false;
                var today = DateTime.UtcNow;
                var age = today.Year - BirthDate.Value.Year;
                if (today.Month < BirthDate.Value.Month) age--;
                return age < CoppaAgeThreshold;
            }
        }

        public DateTime? BirthDate
        {
            get
            {
                if (!PlayerPrefs.HasKey(SaveKeyBirthYear) || !PlayerPrefs.HasKey(SaveKeyBirthMonth))
                    return null;
                int year = PlayerPrefs.GetInt(SaveKeyBirthYear);
                int month = PlayerPrefs.GetInt(SaveKeyBirthMonth);
                if (year < 1900 || year > DateTime.UtcNow.Year || month < 1 || month > 12) return null;
                return new DateTime(year, month, 1);
            }
        }

        public void Verify(DateTime birthDate)
        {
            PlayerPrefs.SetInt(SaveKeyBirthYear, birthDate.Year);
            PlayerPrefs.SetInt(SaveKeyBirthMonth, birthDate.Month);
            PlayerPrefs.Save();
            MoldLogger.LogInfo($"{LogTag} Verified birth year={birthDate.Year} under13={IsUnder13}");
        }

        public void ReVerify(DateTime birthDate) => Verify(birthDate);

        public void Clear()
        {
            PlayerPrefs.DeleteKey(SaveKeyBirthYear);
            PlayerPrefs.DeleteKey(SaveKeyBirthMonth);
            PlayerPrefs.Save();
            MoldLogger.LogInfo($"{LogTag} Cleared.");
        }
    }
}
