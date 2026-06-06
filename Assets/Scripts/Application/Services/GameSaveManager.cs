using System;
using System.Collections.Generic;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Application.Logging;
using PuzzleGame.Domain.Models;
using UnityEngine;

namespace PuzzleGame.Application.Services
{
    /// <summary>
    /// Secure level progress orchestrator. Owns the in-memory cache and the
    /// serialization format; delegates crypto to <see cref="ISaveCrypto"/> and
    /// file IO to <see cref="ISaveStorage"/>. Split from a 384 LOC god-class
    /// in Sprint #18.
    ///
    /// File format:
    ///   - Level states serialized to JSON → payload
    ///   - payload + salt + secretKey → HMAC-SHA256 → signature
    ///   - { version, salt, payload, signature } → JSON → disk
    /// </summary>
    public class GameSaveManager : ISaveManager
    {
        private const int CurrentVersion = 1;
        private const int MaxLevelsInMemory = 1000;

        private readonly ISaveCrypto _crypto;
        private readonly ISaveStorage _storage;

        private SaveData _cachedSaveData;
        private bool _cacheLoaded;

        public GameSaveManager() : this(CreateDefaultCrypto(), CreateDefaultStorage()) { }

        public GameSaveManager(ISaveCrypto crypto, ISaveStorage storage)
        {
            _crypto = crypto ?? throw new ArgumentNullException(nameof(crypto));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        // Reflection-based defaults so the Application layer doesn't take a
        // compile-time dependency on PuzzleGame.Infrastructure. VContainer
        // resolution still uses the concrete types directly.
        private static ISaveCrypto CreateDefaultCrypto()
        {
            var t = Type.GetType("PuzzleGame.Infrastructure.Implementations.SaveCrypto, PuzzleGame.Infrastructure");
            return t != null ? (ISaveCrypto)Activator.CreateInstance(t) : null;
        }

        private static ISaveStorage CreateDefaultStorage()
        {
            var t = Type.GetType("PuzzleGame.Infrastructure.Implementations.SaveStorage, PuzzleGame.Infrastructure");
            return t != null ? (ISaveStorage)Activator.CreateInstance(t) : null;
        }

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
                    var color = new DomainColor(colorR[i], colorG[i], colorB[i], colorA[i]);
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

        [Serializable]
        private class SecureFile
        {
            public int version;
            public string salt;
            public string payload;
            public string signature;
        }

        // ── Public API ──────────────────────────────────────────────────────

        public bool Save(int levelIndex, int moveCount, IMoldView[] Molds, bool isCompleted, int stars)
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
                    data.levels.RemoveAt(0);
                data.levels.Add(levelData);
            }

            return WriteSecure(data);
        }

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
                MoveCount = raw.moveCount,
                IsCompleted = raw.isCompleted,
                Stars = raw.stars,
                SavedAtUnix = raw.savedAtUnix
            };
        }

        public int LoadLastPlayedLevel() => LoadVerified()?.lastPlayedLevel ?? 0;

        public void DeleteAll()
        {
            try
            {
                _cachedSaveData = null;
                _cacheLoaded = false;
                _storage.Delete();
                MoldLogger.LogInfo("[GameSaveManager] All save data deleted.");
            }
            catch (Exception ex)
            {
                MoldLogger.LogError($"[GameSaveManager] Delete failed: {ex.Message}");
            }
        }

        // ── Editor / debug API ──────────────────────────────────────────────

        public static GameSaveManager EditorInstance { get; } = new GameSaveManager();

        public bool HasSaveData => _storage.Exists();

        public bool VerifyIntegrity() => LoadVerified() != null;

        public long FileSizeBytes => _storage.GetSize();

        public string SaveFilePath => _storage.FilePath;

        public SaveData PeekVerified() => LoadVerified();

        // ── Core: verify + write ────────────────────────────────────────────

        private SaveData LoadVerified()
        {
            if (_cacheLoaded) return _cachedSaveData;

            if (!_storage.Exists())
            {
                _cachedSaveData = null;
                _cacheLoaded = true;
                return null;
            }

            try
            {
                var secure = JsonUtility.FromJson<SecureFile>(_storage.ReadAll());
                if (secure == null) return null;

                if (secure.version != CurrentVersion)
                {
                    MoldLogger.LogWarning($"[GameSaveManager] Save version mismatch (got {secure.version}, expected {CurrentVersion}). Discarding.");
                    return null;
                }

                if (!_crypto.Verify(secure.salt, secure.payload, secure.signature))
                {
                    MoldLogger.LogWarning("[GameSaveManager] Save signature invalid — file tampered or corrupted.");
                    return null;
                }

                string payloadJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(secure.payload));
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
                string payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payloadJson));
                string salt = _crypto.GenerateSalt();
                string signature = _crypto.Sign(salt, payload);

                var secure = new SecureFile
                {
                    version = CurrentVersion,
                    salt = salt,
                    payload = payload,
                    signature = signature,
                };

                _storage.WriteAtomic(JsonUtility.ToJson(secure));

                _cachedSaveData = data;
                _cacheLoaded = true;

                MoldLogger.LogInfo($"[GameSaveManager] Saved ({data.levels.Count} levels).");
                return true;
            }
            catch (Exception ex)
            {
                MoldLogger.LogError($"[GameSaveManager] Save failed: {ex.Message}");
                return false;
            }
        }
    }

    // ── Editor-instance default adapters (allow no-arg ctor / static EditorInstance) ──
    // These wrap the concrete Infrastructure types only when no DI container is in play.
    // DI registration uses SaveCrypto / SaveStorage directly via their interfaces.

    // Adapters removed (Sprint #18c): default instances now use reflection
    // (CreateDefaultCrypto/CreateDefaultStorage above) to avoid an
    // Application→Infrastructure compile-time reference.
}
