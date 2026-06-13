using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using UnityEngine;

namespace PuzzleGame.Infrastructure.Implementations
{
    public class PlayerPrefsProgressRepository : IProgressRepository
    {
        private const string XpPrefKey = "PuzzleGame.Progress.TotalXp";
        private const string SeasonXpPrefKey = "PuzzleGame.Progress.SeasonXp";
        private const string ClaimedPrefPrefix = "PuzzleGame.Progress.Claimed.";

        private readonly ISaveCrypto _crypto;

        public PlayerPrefsProgressRepository(ISaveCrypto crypto)
        {
            _crypto = crypto ?? throw new System.ArgumentNullException(nameof(crypto));
        }

        public void LoadProgress(out int totalXp, out int seasonXp, HashSet<int> claimedTiers)
        {
            totalXp = 0;
            seasonXp = 0;

            string payload = PlayerPrefs.GetString(XpPrefKey + "_Data", "");
            string salt = PlayerPrefs.GetString(XpPrefKey + "_Salt", "");
            string sig = PlayerPrefs.GetString(XpPrefKey + "_Sig", "");

            if (!string.IsNullOrEmpty(payload) && _crypto.Verify(salt, payload, sig))
            {
                var parts = payload.Split('_');
                if (parts.Length >= 2)
                {
                    int.TryParse(parts[0], out totalXp);
                    int.TryParse(parts[1], out seasonXp);
                }
            }
            else if (!string.IsNullOrEmpty(payload))
            {
                Application.Logging.MoldLogger.LogError("[PlayerPrefsProgressRepository] XP save data tampered. Resetting to 0.");
            }

            for (int i = 0; i < 200; i++)
            {
                string tierPayload = PlayerPrefs.GetString(ClaimedPrefPrefix + i + "_Data", "");
                string tierSalt = PlayerPrefs.GetString(ClaimedPrefPrefix + i + "_Salt", "");
                string tierSig = PlayerPrefs.GetString(ClaimedPrefPrefix + i + "_Sig", "");

                if (!string.IsNullOrEmpty(tierPayload) && _crypto.Verify(tierSalt, tierPayload, tierSig))
                {
                    claimedTiers.Add(i);
                }
            }
        }

        public void SaveXp(int totalXp, int seasonXp)
        {
            string payload = $"{totalXp}_{seasonXp}";
            string salt = _crypto.GenerateSalt();
            string signature = _crypto.Sign(salt, payload);

            PlayerPrefs.SetString(XpPrefKey + "_Data", payload);
            PlayerPrefs.SetString(XpPrefKey + "_Salt", salt);
            PlayerPrefs.SetString(XpPrefKey + "_Sig", signature);
            PlayerPrefs.Save();
        }

        public void SaveClaimedTier(int tierIndex)
        {
            string payload = "claimed";
            string salt = _crypto.GenerateSalt();
            string signature = _crypto.Sign(salt, payload);

            PlayerPrefs.SetString(ClaimedPrefPrefix + tierIndex + "_Data", payload);
            PlayerPrefs.SetString(ClaimedPrefPrefix + tierIndex + "_Salt", salt);
            PlayerPrefs.SetString(ClaimedPrefPrefix + tierIndex + "_Sig", signature);
            PlayerPrefs.Save();
        }

        public void ResetProgress()
        {
            PlayerPrefs.DeleteKey(XpPrefKey + "_Data");
            PlayerPrefs.DeleteKey(XpPrefKey + "_Salt");
            PlayerPrefs.DeleteKey(XpPrefKey + "_Sig");
            for (int i = 0; i < 200; i++)
            {
                PlayerPrefs.DeleteKey(ClaimedPrefPrefix + i + "_Data");
                PlayerPrefs.DeleteKey(ClaimedPrefPrefix + i + "_Salt");
                PlayerPrefs.DeleteKey(ClaimedPrefPrefix + i + "_Sig");
            }
            PlayerPrefs.Save();
        }
    }
}
