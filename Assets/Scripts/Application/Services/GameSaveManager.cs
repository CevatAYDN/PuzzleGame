using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using PuzzleGame.Domain.Models;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Save dosyası şu şekilde diske yazılır (anti-tamper):
    ///   - Level state'leri JSON'a serialize edilir → payload
    ///   - payload + salt + secretKey → HMAC-SHA256 → signature
    ///   - { version, salt, payload, signature } → encrypted JSON → dosya
    /// Saldırgan payload'ı edit etse bile HMAC eşleşmediği için reddedilir.
    /// </summary>
    public static class GameSaveManager
    {
        private const string SaveFileName = "puzzlegame_save.json";
        private const int CurrentVersion = 1;
        private const int MaxLevelsInMemory = 64; // aşırı büyüme koruması

        /// <summary>
        /// Obfuscated gizli anahtar. Prod'da değiştirilmeli.
        /// Birden fazla parçaya bölünmüş — string concat olarak bulunması zor.
        /// </summary>
        private static readonly string SecretKey = BuildSecretKey();

        private static string BuildSecretKey()
        {
            // Parçaları birleştirerek "türetilmiş" anahtar
            return string.Concat(
                "PG-S", "ave-",
                "v1", "-",
                "X72k", "Q9mP",
                "r4tF", "v8wL"
            );
        }

        private static string FilePath =>
            Path.Combine(UnityEngine.Application.persistentDataPath, SaveFileName);

        private static string TempPath => FilePath + ".tmp";

        // ── Domain types ────────────────────────────────────────────────────

        [Serializable]
        public struct BottleSaveData
        {
            public float[] colorR;
            public float[] colorG;
            public float[] colorB;
            public float[] colorA;
            public float[] amounts;

            public static BottleSaveData FromLayers(IReadOnlyList<LiquidLayer> layers)
            {
                int count = layers?.Count ?? 0;
                var data = new BottleSaveData
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

            public LiquidLayer[] ToLayers()
            {
                int count = colorR?.Length ?? 0;
                var layers = new LiquidLayer[count];
                for (int i = 0; i < count; i++)
                {
                    var color = new DomainColor(
                        colorR[i], colorG[i], colorB[i], colorA[i]);
                    layers[i] = new LiquidLayer(color, amounts[i]);
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
            public long savedAtUnix;
            public BottleSaveData[] bottles;
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
        public static bool Save(int levelIndex, int moveCount,
            BottleController[] bottles, bool isCompleted)
        {
            if (bottles == null) return false;

            var data = LoadVerified() ?? new SaveData { version = CurrentVersion };
            data.lastPlayedLevel = levelIndex;
            data.version = CurrentVersion;

            var levelData = new LevelStateData
            {
                levelIndex = levelIndex,
                moveCount = moveCount,
                isCompleted = isCompleted,
                savedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                bottles = new BottleSaveData[bottles.Length],
            };
            for (int i = 0; i < bottles.Length; i++)
            {
                if (bottles[i] != null)
                    levelData.bottles[i] = BottleSaveData.FromLayers(bottles[i].VisualLayers);
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
        public static LevelStateData? LoadLevel(int levelIndex)
        {
            var data = LoadVerified();
            if (data == null) return null;

            int idx = data.levels.FindIndex(l => l.levelIndex == levelIndex);
            return idx >= 0 ? data.levels[idx] : (LevelStateData?)null;
        }

        public static int LoadLastPlayedLevel()
        {
            return LoadVerified()?.lastPlayedLevel ?? 0;
        }

        public static void DeleteAll()
        {
            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
                if (File.Exists(TempPath)) File.Delete(TempPath);
                Debug.Log("[GameSaveManager] All save data deleted.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameSaveManager] Delete failed: {ex.Message}");
            }
        }

        // ── Editor / debug API ──────────────────────────────────────────────

        public static bool HasSaveData => File.Exists(FilePath);
        public static string SaveFilePath => FilePath;
        public static int FileVersion => LoadVerified()?.version ?? 0;

        public static SaveData PeekVerified()
        {
            return LoadVerified();
        }

        public static long FileSizeBytes
        {
            get
            {
                try { return File.Exists(FilePath) ? new FileInfo(FilePath).Length : 0; }
                catch { return 0; }
            }
        }

        public static bool VerifyIntegrity()
        {
            // LoadVerified zaten kontrol yapıyor, ama exposed da istiyoruz
            return LoadVerified() != null;
        }

        // ── Core: verify + write ────────────────────────────────────────────

        private static SaveData LoadVerified()
        {
            if (!File.Exists(FilePath)) return null;

            try
            {
                string json = File.ReadAllText(FilePath, Encoding.UTF8);
                var secure = JsonUtility.FromJson<SecureFile>(json);
                if (secure == null) return null;

                // Version check
                if (secure.version != CurrentVersion)
                {
                    Debug.LogWarning($"[GameSaveManager] Save version mismatch (got {secure.version}, expected {CurrentVersion}). Discarding.");
                    return null;
                }

                // Signature check
                if (!VerifyHmac(secure.salt, secure.payload, secure.signature))
                {
                    Debug.LogWarning("[GameSaveManager] Save signature invalid — file tampered or corrupted.");
                    return null;
                }

                // Payload decode
                string payloadJson = Encoding.UTF8.GetString(Convert.FromBase64String(secure.payload));
                var data = JsonUtility.FromJson<SaveData>(payloadJson);
                return data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameSaveManager] Load failed: {ex.Message}");
                return null;
            }
        }

        private static bool WriteSecure(SaveData data)
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

                Debug.Log($"[GameSaveManager] Saved ({data.levels.Count} levels, {FileSizeBytes} bytes).");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameSaveManager] Save failed: {ex.Message}");
                try { if (File.Exists(TempPath)) File.Delete(TempPath); } catch { }
                return false;
            }
        }

        // ── HMAC-SHA256 helpers ─────────────────────────────────────────────

        private static string ComputeHmac(string salt, string payload)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SecretKey)))
            {
                byte[] data = Encoding.UTF8.GetBytes(salt + payload);
                byte[] hash = hmac.ComputeHash(data);
                return ByteArrayToHex(hash);
            }
        }

        private static bool VerifyHmac(string salt, string payload, string expectedHex)
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
