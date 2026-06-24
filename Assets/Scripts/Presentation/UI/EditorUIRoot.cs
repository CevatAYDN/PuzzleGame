using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using Nexus.Core;
using PuzzleGame.Application.Events;
using PuzzleGame.Application.Configuration;
using PuzzleGame.Application.Interfaces;
using PuzzleGame.Domain.Interfaces;
using PuzzleGame.Domain.Models;

namespace PuzzleGame.Presentation.UI
{
    [ExecuteInEditMode]
    public class EditorUIRoot : MonoBehaviour
    {
        private NexusDI _resolver;
        private bool _initialized;

        private void Awake()
        {
            if (UnityEngine.Application.isPlaying) return;
            Initialize();
        }

        private void Update()
        {
            if (UnityEngine.Application.isPlaying && _initialized)
            {
                DestroyImmediate(this);
            }
        }

        private void Initialize()
        {
            if (_initialized || UnityEngine.Application.isPlaying) return;
            if (this == null) return;
            _initialized = true;

            _resolver = new NexusDI();

            _resolver.BindInstance<IEventAggregator>(new EventAggregator());
            _resolver.BindInstance<ICoinWallet>(new EditorCoinWallet());
            _resolver.BindInstance<IStreakService>(new EditorStreakService());
            _resolver.BindInstance<IDailyChallengeService>(new EditorDailyChallengeService());
            _resolver.BindInstance<ILocalizationService>(new EditorLocalizationService());
            _resolver.BindInstance<IHapticFeedbackService>(new EditorHapticService());
            _resolver.BindInstance<ILevelProgressService>(new EditorLevelProgressService());
            _resolver.BindInstance<IAudioSettingsService>(new EditorAudioSettingsService());
            _resolver.BindInstance<IAudioService>(new EditorAudioService());
            _resolver.BindInstance<IConsentManager>(new EditorConsentManager());
            _resolver.BindInstance<IAdService>(new EditorAdService());
            _resolver.BindInstance<IAnalyticsService>(new EditorAnalyticsService());
            _resolver.BindInstance<IAgeVerificationService>(new EditorAgeVerificationService());
            _resolver.BindInstance<ILevelRepository>(new EditorLevelRepository());
            _resolver.BindInstance<ITweenService>(new EditorTweenService());

            var controllers = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
            foreach (var ctrl in controllers)
            {
                if (!IsKnownController(ctrl)) continue;

                _resolver.Inject(ctrl);
                Debug.Log($"[EditorUIRoot] Injected {ctrl.GetType().Name}");

                // Use reflection instead of SendMessage to bypass Unity's
                // internal ShouldRunBehaviour() assertion, which fires for
                // lifecycle messages (Start, OnDisable, OnEnable) sent to
                // non-[ExecuteInEditMode] components in edit mode.
                InvokeMethod(ctrl, "OnDisable");
                InvokeMethod(ctrl, "OnEnable");
                InvokeMethod(ctrl, "Start");

                Debug.Log($"[EditorUIRoot] Awakened {ctrl.GetType().Name}");
            }

            Debug.Log("[EditorUIRoot] All UI controllers initialized for editor mode.");
        }

        private static void InvokeMethod(MonoBehaviour ctrl, string methodName)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            ctrl.GetType().GetMethod(methodName, flags)?.Invoke(ctrl, null);
        }

        private static bool IsKnownController(MonoBehaviour ctrl)
        {
            var t = ctrl.GetType();
            return t == typeof(MainMenuController)
                || t == typeof(SettingsController)
                || t == typeof(SettingsPrivacyController)
                || t == typeof(SettingsSoundController)
                || t == typeof(WorldMapController)
                || t == typeof(DailyChallengeController)
                || t == typeof(LevelSelectUI);
        }

        // ──────────────────────────────────────────────────────────────────────
        //  Editor Mock Implementations
        // ──────────────────────────────────────────────────────────────────────

        private sealed class EditorCoinWallet : ICoinWallet
        {
            public int Balance { get; private set; } = 9999;
            public event Action<int> OnBalanceChanged;

            public bool CanAfford(int amount) => Balance >= amount;
            public void Add(int amount, string reason) { Balance += amount; OnBalanceChanged?.Invoke(Balance); }
            public bool TrySpend(int amount, string reason)
            {
                if (Balance < amount) return false;
                Balance -= amount;
                OnBalanceChanged?.Invoke(Balance);
                return true;
            }
        }

        private sealed class EditorStreakService : IStreakService
        {
            public int CurrentStreak => 7;
            public int LongestStreak => 14;
            public DateTime? LastClaimedUtc => DateTime.UtcNow.AddDays(-1);
            public bool IsClaimableToday => true;
            public bool TryClaim() => true;
        }

        private sealed class EditorDailyChallengeService : IDailyChallengeService
        {
            public DailyChallengeState GetTodayChallenge() => new DailyChallengeState
            {
                HasChallenge = true,
                Seed = 42,
                IssuedAtUtc = DateTime.UtcNow.Date,
                Completed = false
            };
            public void MarkCompleted() { }
            public void Reset() { }
        }

        private sealed class EditorLocalizationService : ILocalizationService
        {
            public SupportedLanguage CurrentLanguage { get; set; } = SupportedLanguage.English;
            public string GetString(string key) => $"[{key}]";
            public void SetLanguage(SupportedLanguage language) { CurrentLanguage = language; }
            public void AddTranslation(string key, SupportedLanguage language, string value) { }
            public string GetStringOrDefault(string key, string fallback) => $"[{key}]";
        }

        private sealed class EditorHapticService : IHapticFeedbackService
        {
            public bool IsEnabled { get; set; } = true;
            public void Trigger(HapticIntensity intensity) { }
        }

        private sealed class EditorLevelProgressService : ILevelProgressService
        {
            public bool IsUnlocked(int levelNumber) => levelNumber <= 3;
            public int GetStars(int levelNumber) => levelNumber <= 3 ? 3 : 0;
            public int GetBestMoves(int levelNumber) => levelNumber;
            public bool IsCompleted(int levelNumber) => levelNumber <= 3;
            public void RecordCompletion(int levelNumber, int moveCount, int stars) { }
            public void ResetAll() { }
        }

        private sealed class EditorAudioSettingsService : IAudioSettingsService
        {
            private AudioPreferences _prefs = AudioPreferences.Default;
            public AudioPreferences Current => _prefs;
            public event Action<AudioPreferences> OnSettingsChanged;

            public void SetMusicEnabled(bool enabled)
            {
                _prefs = _prefs.WithMusicEnabled(enabled);
                OnSettingsChanged?.Invoke(_prefs);
            }
            public void SetSfxEnabled(bool enabled)
            {
                _prefs = _prefs.WithSfxEnabled(enabled);
                OnSettingsChanged?.Invoke(_prefs);
            }
            public void SetMusicVolume(float volume)
            {
                _prefs = _prefs.WithMusicVolume(volume);
                OnSettingsChanged?.Invoke(_prefs);
            }
            public void SetSfxVolume(float volume)
            {
                _prefs = _prefs.WithSfxVolume(volume);
                OnSettingsChanged?.Invoke(_prefs);
            }
            public void Save() { }
            public void ResetToDefaults()
            {
                _prefs = AudioPreferences.Default;
                OnSettingsChanged?.Invoke(_prefs);
            }
        }

        private sealed class EditorAudioService : IAudioService
        {
            public float MasterVolume { get; set; } = 1f;
            public float MusicVolume { get; set; } = 0.6f;
            public float SfxVolume { get; set; } = 0.8f;
            public void PlaySfx(AudioClipId id) { }
            public void PlaySfxAt(AudioClipId id, Vector3 worldPos) { }
            public void PlayMusic(AudioClipId id, bool loop = true) { }
            public void StopMusic(float fadeOut = 0.5f) { }
            public void MuteAll() { }
            public void UnmuteAll() { }
            public void ReleaseAll() { }
        }

        private sealed class EditorConsentManager : IConsentManager
        {
            public AdConsentState ConsentState => AdConsentState.Accepted;
            public bool IsReady => true;
            public bool IsUnder13 => false;
            public void Initialize(bool isUnder13) { }
            public void RequestConsentIfNeeded(Action<AdConsentState> onComplete) { onComplete?.Invoke(AdConsentState.Accepted); }
            public void SetConsent(AdConsentState state, bool personalizedAds) { }
            public void ResetConsent() { }
        }

        private sealed class EditorAdService : IAdService
        {
            public bool IsInitialized => true;
            public bool IsPersonalizedAdsEnabled { get; set; } = true;
            public AdConsentState ConsentState => AdConsentState.Accepted;

            public void Initialize() { }
            public void SetConsentState(AdConsentState state, bool personalizedAds) { IsPersonalizedAdsEnabled = personalizedAds; }
            public bool IsRewardedAdReady(RewardedAdType type) => false;
            public void ShowRewardedAd(RewardedAdType type, Action<bool> onComplete) { onComplete?.Invoke(false); }
            public bool IsInterstitialReady() => false;
            public void ShowInterstitialAd(Action onComplete) { onComplete?.Invoke(); }
            public void PreloadAds() { }
        }

        private sealed class EditorAnalyticsService : IAnalyticsService
        {
            public bool IsEnabled { get; set; } = true;
            public void Track(AnalyticsEvent evt, System.Collections.Generic.IReadOnlyDictionary<string, object> properties = null) { }
            public void SetUserProperty(string key, string value) { }
            public void Flush() { }
        }

        private sealed class EditorAgeVerificationService : IAgeVerificationService
        {
            public bool IsVerified => true;
            public bool IsUnder13 => false;
            public DateTime? BirthDate => new DateTime(2000, 1, 1);
            public void Verify(DateTime birthDate) { }
            public void ReVerify(DateTime birthDate) { }
            public void Clear() { }
        }

        private sealed class EditorLevelRepository : ILevelRepository
        {
            public IReadOnlyList<LevelData> AllLevels => Array.Empty<LevelData>();
            public int TotalCount => 0;
            public LevelData GetByNumber(int levelNumber) => null;
        }

        private sealed class EditorTweenService : ITweenService
        {
            public ITweenHandle TweenPosition(Transform t, Vector3 target, float duration, EaseType ease) => new NoOpHandle();
            public ITweenHandle TweenLocalPosition(Transform t, Vector3 target, float duration, EaseType ease) => new NoOpHandle();
            public ITweenHandle TweenScale(Transform t, Vector3 target, float duration, EaseType ease) => new NoOpHandle();
            public ITweenHandle TweenRotation(Transform t, Vector3 target, float duration, EaseType ease) => new NoOpHandle();
            public ITweenHandle TweenShakeRotation(Transform t, float duration, Vector3 strength, int vibrato) => new NoOpHandle();
            public ITweenHandle TweenCustom(object target, float from, float to, float duration, Action<object, float> onUpdate)
            {
                onUpdate?.Invoke(target, to);
                return new NoOpHandle();
            }
            public ITweenHandle SequenceCreate() => new NoOpHandle();
            public ITweenHandle Delay(float duration) => new NoOpHandle();
            public void StopAll(Transform target) { }

            private sealed class NoOpHandle : ITweenHandle
            {
                public void Chain(ITweenHandle other) { }
                public void Group(ITweenHandle other) { }
                public ITweenHandle OnComplete(Action callback) { callback?.Invoke(); return this; }
                public void Kill() { }
                public void Start() { }
            }
        }
    }
}
