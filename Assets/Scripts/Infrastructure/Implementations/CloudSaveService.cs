using System;
using System.Collections.Generic;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using UnityEngine;

namespace PuzzleGame.Infrastructure.Implementations
{
    public sealed class CloudSaveService : ICloudSaveService
    {
        private const string LogTag = "[CloudSave]";
        private const string TimestampPrefKey = "PuzzleGame.CloudSave.Timestamp";

        private readonly ILeaderboardService _leaderboard;
        private readonly IProgressService _progress;
        private readonly ICosmeticShopService _shop;
        private readonly ISaveCrypto _crypto;
        private readonly ISaveStorage _storage;
        private long _lastSavedAtUnix;

        public bool HasCloudSave => _storage.Exists();
        public long LastSavedAtUnix => _lastSavedAtUnix;

        public CloudSaveService(
            ILeaderboardService leaderboard,
            IProgressService progress,
            ICosmeticShopService shop,
            ISaveCrypto crypto,
            ISaveStorage storage)
        {
            _leaderboard = leaderboard ?? throw new ArgumentNullException(nameof(leaderboard));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _shop = shop ?? throw new ArgumentNullException(nameof(shop));
            _crypto = crypto ?? throw new ArgumentNullException(nameof(crypto));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _lastSavedAtUnix = long.Parse(PlayerPrefs.GetString(TimestampPrefKey, "0"));
        }

        public string SerializeSnapshot()
        {
            var data = BuildSnapshot();
            var wrapper = new CloudSaveDataWrapper { data = data };
            return JsonUtility.ToJson(wrapper);
        }

        public CloudSaveData DeserializeSnapshot(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                var wrapper = JsonUtility.FromJson<CloudSaveDataWrapper>(json);
                return wrapper?.data;
            }
            catch (Exception ex)
            {
                MoldLogger.LogError($"{LogTag} Deserialize failed: {ex.Message}");
                return null;
            }
        }

        public void SaveToCloud()
        {
            try
            {
                var payload = SerializeSnapshot();
                if (string.IsNullOrEmpty(payload))
                {
                    MoldLogger.LogError($"{LogTag} Save aborted: empty payload.");
                    return;
                }

                var salt = _crypto.GenerateSalt();
                var signature = _crypto.Sign(salt, payload);

                var envelope = new CloudSaveEnvelope
                {
                    version = 1,
                    salt = salt,
                    payload = payload,
                    signature = signature,
                    savedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                var json = JsonUtility.ToJson(envelope);
                _storage.WriteAtomic(json);

                _lastSavedAtUnix = envelope.savedAtUnix;
                PlayerPrefs.SetString(TimestampPrefKey, _lastSavedAtUnix.ToString());
                PlayerPrefs.Save();

                MoldLogger.LogInfo($"{LogTag} Saved to cloud slot ({payload.Length} chars).");
            }
            catch (Exception ex)
            {
                MoldLogger.LogError($"{LogTag} Save failed: {ex.Message}");
            }
        }

        public bool LoadFromCloud()
        {
            try
            {
                if (!_storage.Exists())
                {
                    MoldLogger.LogWarning($"{LogTag} No cloud save found.");
                    return false;
                }

                var json = _storage.ReadAll();
                if (string.IsNullOrEmpty(json))
                {
                    MoldLogger.LogError($"{LogTag} Cloud save file is empty.");
                    return false;
                }

                var envelope = JsonUtility.FromJson<CloudSaveEnvelope>(json);
                if (envelope == null || string.IsNullOrEmpty(envelope.payload))
                {
                    MoldLogger.LogError($"{LogTag} Invalid envelope.");
                    return false;
                }

                if (!_crypto.Verify(envelope.salt, envelope.payload, envelope.signature))
                {
                    MoldLogger.LogError($"{LogTag} Signature verification FAILED. Data may be tampered.");
                    return false;
                }

                var data = DeserializeSnapshot(envelope.payload);
                if (data == null)
                {
                    MoldLogger.LogError($"{LogTag} Payload deserialization failed.");
                    return false;
                }

                RestoreToPlayerPrefs(data);
                _lastSavedAtUnix = envelope.savedAtUnix;
                PlayerPrefs.SetString(TimestampPrefKey, _lastSavedAtUnix.ToString());
                PlayerPrefs.Save();
                MoldLogger.LogInfo($"{LogTag} Loaded from cloud slot ({json.Length} chars).");
                return true;
            }
            catch (Exception ex)
            {
                MoldLogger.LogError($"{LogTag} Load failed: {ex.Message}");
                return false;
            }
        }

        public void DeleteCloudSave()
        {
            _storage.Delete();
            _lastSavedAtUnix = 0;
            PlayerPrefs.DeleteKey(TimestampPrefKey);
            PlayerPrefs.Save();
            MoldLogger.LogInfo($"{LogTag} Cloud save deleted.");
        }

        private CloudSaveData BuildSnapshot()
        {
            var data = new CloudSaveData
            {
                savedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            foreach (var entry in _leaderboard.GetAllEntries())
            {
                data.leaderboardEntries.Add(new CloudLeaderboardEntry
                {
                    levelIndex = entry.LevelIndex,
                    bestScore = entry.BestScore,
                    bestPourCount = entry.BestPourCount,
                    recordedAtUnix = entry.RecordedAtUnix
                });
            }

            data.claimedTierIds.AddRange(_progress.GetClaimedTierIds());

            foreach (var id in _shop.GetOwnedItemIds())
                data.ownedCosmeticIds.Add(id);

            foreach (CosmeticType type in Enum.GetValues(typeof(CosmeticType)))
            {
                if (type == CosmeticType.None) continue;
                var equipped = _shop.GetEquipped(type);
                if (!string.IsNullOrEmpty(equipped))
                {
                    data.equippedCosmetics.Add(new CloudEquippedCosmetic
                    {
                        cosmeticType = (int)type,
                        itemId = equipped
                    });
                }
            }

            return data;
        }

        private static void RestoreToPlayerPrefs(CloudSaveData data)
        {
            foreach (var entry in data.leaderboardEntries)
            {
                PlayerPrefs.SetInt("PuzzleGame.Leaderboard.Score." + entry.levelIndex, entry.bestScore);
                PlayerPrefs.SetInt("PuzzleGame.Leaderboard.Pour." + entry.levelIndex, entry.bestPourCount);
                PlayerPrefs.SetString("PuzzleGame.Leaderboard.Time." + entry.levelIndex, entry.recordedAtUnix.ToString());
            }

            PlayerPrefs.SetInt("PuzzleGame.Progress.TotalXp", data.totalXp);
            PlayerPrefs.SetInt("PuzzleGame.Progress.SeasonXp", data.seasonXp);

            foreach (var tierId in data.claimedTierIds)
                PlayerPrefs.SetInt("PuzzleGame.Progress.Claimed." + tierId, 1);

            foreach (var id in data.ownedCosmeticIds)
                PlayerPrefs.SetInt("PuzzleGame.Cosmetic.Owned." + id, 1);

            foreach (var eq in data.equippedCosmetics)
                PlayerPrefs.SetString("PuzzleGame.Cosmetic.Equipped." + eq.cosmeticType, eq.itemId);

            PlayerPrefs.Save();
            MoldLogger.LogInfo($"{LogTag} Restored {data.leaderboardEntries.Count} leaderboard entries, " +
                               $"{data.claimedTierIds.Count} claimed tiers, " +
                               $"{data.ownedCosmeticIds.Count} owned cosmetics.");
        }

        [Serializable]
        private sealed class CloudSaveDataWrapper
        {
            public CloudSaveData data;
        }

        [Serializable]
        private sealed class CloudSaveEnvelope
        {
            public int version;
            public string salt;
            public string payload;
            public string signature;
            public long savedAtUnix;
        }
    }
}
