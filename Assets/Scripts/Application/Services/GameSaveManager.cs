using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using PuzzleGame.Domain.Models;
using UnityEngine;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// HMAC-SHA256 anti-tamper save manager.
    /// Converted from static class to injectable instance (Fix #1 — Critical).
    /// Register via DI: builder.Register&lt;ISaveManager, GameSaveManager&gt;(Lifetime.Singleton)
    ///
    /// File format:
    ///   - Level states serialized to JSON → payload
    ///   - payload + salt + secretKey → HMAC-SHA256 → signature
    ///   - { version, salt, payload, signature } → JSON → disk
    /// </summary>
    public class GameSaveManager : ISaveManager
    {
        private const string SaveFileName = "puzzlegame_save.json";
        private const int CurrentVersion = 1;
        private const int MaxLevelsInMemory = 1000;

        private readonly string SecretKey = BuildSecretKey();

        private SaveData _cachedSaveData;
        private bool _cacheLoaded;

        private static string BuildSecretKey()
        {
            // Cihaz-bağımlı tuz + statik biber.
            // Tuz: her kurulumda farklıdır (device unique id + cihaz adı).
            // Biber: tersine mühendisliği zorlaştırmak için kodun içinde tutulur.
            // Tüm tuzun kaybolması (yeni cihaz, format) eski save'lerin reddedilmesine yol açar — kabul edilebilir trade-off.
            string deviceId = SystemInfo.deviceUniqueIdentifier ?? "fallback";
            string deviceModel = SystemInfo.deviceModel ?? "unknown";
            const string pepper = "PG-Save-v1-X72kQ9mPr4tFv8wL";
            return $"{deviceId}:{deviceModel}:{pepper}";
        }

        private string FilePath =>
            Path.Combine(UnityEngine.Application.persistentDataPath, SaveFileName);

        private string TempPath => FilePath + ".tmp";

        // ── Domain types ────────────────────────────────────────────────────

        [Serializable]
        public struct MoldSaveData
        {
            public float[] colorR;
            public float[] colorG;
            public float[] colorB;
            public float[] colorA;
            public float[] amounts;

            public static MoldSaveData FromLayers(IReadOnlyList<OreLayer> layers)
            {
                int count = layers?.Count ?? 0;
                var data = new MoldSaveData
                {
                    colorR = new float[count],
                    colorG = new float[count],
                    colorB = new float[count],
                    colorA = new float[count],
                    amounts = new float[count],
                };
                for (int i = 0; i < count; i++)
                {
                    var layer = layers[i];
                    data.colorR[i] = layer.Color.R;
                    data.colorG[i] = layer.Color.G;
                    data.colorB[i] = layer.Color.B;
                    data.colorA[i] = layer.Color.A;
                    data.amounts[i] = layer.Amount;
                }
                return data;
            }

            public OreLayer[] ToLayers()
            {
                int count = colorR?.Length ?? 0;
                var layers = new OreLayer[count];
                for (int i = 0; i < count; i++)
                {
                    var color = new DomainColor(
                        colorR[i], colorG[i], colorB[i], colorA[i]);
                    layers[i] = new OreLayer(color, amounts[i]);
                }
                return layers;
            }
        }

        [Serializable]
        public struct LevelStateData
        {
            public int levelIndex;
            public int moveCount;
            public bool isCompleted;
            public int stars;
            public long savedAtUnix;
            public MoldSaveData[] Molds;
        }

        [Serializable]
        public class SaveData
        {
            public int lastPlayedLevel;
            public int version;
            public List<LevelStateData> levels = new List<LevelStateData>();
        }

        // Save dosya formatı: içeriği değiştirilmiş payload'ı korur
        [Serializable]
        private class SecureFile
        {
            public int version;
            public string salt;
            public string payload; // base64(utf8(json of SaveData))
            public string signature; // hex of HMAC-SHA256
        }

        // ── Public API ──────────────────────────────────────────────────────

        /// <summary>
        /// Level state'ini kaydeder. Atomic write kullanır (yarım kalmış dosya oluşmaz).
        /// </summary>
        public bool Save(int levelIndex, int moveCount,
            IMoldView[] Molds, bool isCompleted, int stars)
        {
            if (Molds == null) return false;

            var data = LoadVerified() ?? new SaveData { version = CurrentVersion };
            data.lastPlayedLevel = levelIndex;
            data.version = CurrentVersion;

            var levelData = new LevelStateData
            {
                levelIndex = levelIndex,
                moveCount = moveCount,
                isCompleted = isCompleted,
                stars = stars,
                savedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Molds = new MoldSaveData[Molds.Length],
            };
            for (int i = 0; i < Molds.Length; i++)
            {
                if (Molds[i] != null)
                    levelData.Molds[i] = MoldSaveData.FromLayers(Molds[i].VisualLayers);
            }

            int existing = data.levels.FindIndex(l => l.levelIndex == levelIndex);
            if (existing >= 0) data.levels[existing] = levelData;
            else
            {
                if (data.levels.Count >= MaxLevelsInMemory)
                    data.levels.RemoveAt(0); // eski level'ı at
                data.levels.Add(levelData);
            }

            return WriteSecure(data);
        }

        /// <summary>
        /// Belirtilen level'ın doğrulanmış kaydını döndürür.
        /// Dosya yoksa, signature yanlışsa, JSON bozuksa null döner.
        /// </summary>
        public GameSaveData? LoadLevel(int levelIndex)
        {
            var data = LoadVerified();
            if (data == null) return null;
            int idx = data.levels.FindIndex(l => l.levelIndex == levelIndex);
            if (idx < 0) return null;
            var raw = data.levels[idx];
            return new GameSaveData
            {
                LevelIndex = raw.levelIndex,
                MoveCount  = raw.moveCount,
                IsCompleted = raw.isCompleted,
                Stars = raw.stars,
                SavedAtUnix = raw.savedAtUnix
            };
        }

        public int LoadLastPlayedLevel()
        {
            return LoadVerified()?.lastPlayedLevel ?? 0;
        }

        public void DeleteAll()
        {
            try
            {
                _cachedSaveData = null;
                _cacheLoaded = false;
                if (File.Exists(FilePath)) File.Delete(FilePath);
                if (File.Exists(TempPath)) File.Delete(TempPath);
                MoldLogger.LogInfo("[GameSaveManager] All save data deleted.");
            }
            catch (Exception ex)
            {
                MoldLogger.LogError($"[GameSaveManager] Delete failed: {ex.Message}");
            }
        }

        // ── Editor / debug API ──────────────────────────────────────────────

        public static GameSaveManager EditorInstance { get; } = new GameSaveManager();

        public bool HasSaveData => File.Exists(FilePath);

        public bool VerifyIntegrity() => LoadVerified() != null;

        public long FileSizeBytes
        {
            get
            {
                try
                {
                    if (File.Exists(FilePath)) return new FileInfo(FilePath).Length;
                }
                catch (Exception ex)
                {
                    MoldLogger.LogWarning($"[GameSaveManager] FileSizeBytes failed: {ex.Message}");
                }
                return 0;
            }
        }

        public string SaveFilePath => FilePath;

        public SaveData PeekVerified()
        {
            return LoadVerified();
        }

        // ── Core: verify + write ────────────────────────────────────────────

        private SaveData LoadVerified()
        {
            if (_cacheLoaded)
            {
                return _cachedSaveData;
            }

            if (!File.Exists(FilePath))
            {
                _cachedSaveData = null;
                _cacheLoaded = true;
                return null;
            }

            try
            {
                string json = File.ReadAllText(FilePath, Encoding.UTF8);
                var secure = JsonUtility.FromJson<SecureFile>(json);
                if (secure == null) return null;

                // Version check
                if (secure.version != CurrentVersion)
                {
                    MoldLogger.LogWarning($"[GameSaveManager] Save version mismatch (got {secure.version}, expected {CurrentVersion}). Discarding.");
                    return null;
                }

                // Signature check
                if (!VerifyHmac(secure.salt, secure.payload, secure.signature))
                {
                    MoldLogger.LogWarning("[GameSaveManager] Save signature invalid — file tampered or corrupted.");
                    return null;
                }

                // Payload decode
                string payloadJson = Encoding.UTF8.GetString(Convert.FromBase64String(secure.payload));
                var data = JsonUtility.FromJson<SaveData>(payloadJson);
                _cachedSaveData = data;
                _cacheLoaded = true;
                return data;
            }
            catch (Exception ex)
            {
                MoldLogger.LogError($"[GameSaveManager] Load failed: {ex.Message}");
                return null;
            }
        }

        private bool WriteSecure(SaveData data)
        {
            try
            {
                string payloadJson = JsonUtility.ToJson(data);
                string payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson));
                string salt = GenerateSalt();
                string signature = ComputeHmac(salt, payload);

                var secure = new SecureFile
                {
                    version = CurrentVersion,
                    salt = salt,
                    payload = payload,
                    signature = signature,
                };
                string json = JsonUtility.ToJson(secure);

                // Atomic write: temp'e yaz, sonra rename
                string dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(TempPath, json, Encoding.UTF8);
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(TempPath, FilePath);

                // Update cache
                _cachedSaveData = data;
                _cacheLoaded = true;

                MoldLogger.LogInfo($"[GameSaveManager] Saved ({data.levels.Count} levels).");
                return true;
            }
            catch (Exception ex)
            {
                MoldLogger.LogError($"[GameSaveManager] Save failed: {ex.Message}");
                try 
                { 
                    if (File.Exists(TempPath)) File.Delete(TempPath); 
                } 
                catch (Exception deleteEx)
                {
                    MoldLogger.LogWarning($"[GameSaveManager] Temp file delete failed: {deleteEx.Message}");
                }
                return false;
            }
        }

        // ── HMAC-SHA256 helpers ─────────────────────────────────────────────

        private string ComputeHmac(string salt, string payload)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SecretKey)))
            {
                byte[] data = Encoding.UTF8.GetBytes(salt + payload);
                byte[] hash = hmac.ComputeHash(data);
                return ByteArrayToHex(hash);
            }
        }

        private bool VerifyHmac(string salt, string payload, string expectedHex)
        {
            string actual = ComputeHmac(salt, payload);
            // Constant-time comparison — timing attack koruması
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

        private static string GenerateSalt()
        {
            var bytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        private static string ByteArrayToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
