# Ore Sorter — Technical Architecture Document

**Versiyon:** 1.0
**Tarih:** 2026-06-05

---

## 1. Mimari Genel Bakış

### 1.1 Clean Architecture Katmanları

```
┌─────────────────────────────────────────────────────┐
│ Presentation (MonoBehaviours, Scenes)               │
│   - GameManager, MoldController, HudPresenter       │
│   - WinLoseEvaluator, LevelFlowController           │
│   - DebugOverlayUI, ScreenTransitionService         │
└────────────┬────────────────────────────────────────┘
             │ depends on ↓
┌────────────┴────────────────────────────────────────┐
│ Application (Use Cases, Orchestration)              │
│   - Services: CastService, HintService, UndoService │
│   - CoinWallet, TutorialService, DailyChallenge     │
│   - HapticFeedbackService, ScreenTransition         │
│   - Interfaces: IMoldView, IActiveMoldsProvider     │
└────────────┬────────────────────────────────────────┘
             │ depends on ↓
┌────────────┴────────────────────────────────────────┐
│ Domain (Pure Business Logic, Unity-agnostic)        │
│   - Models: MoldState, OreLayer, LevelData          │
│   - Services: OreSortSolver, LevelGenerator         │
│   - Interfaces: ITweenService, IRendererService     │
└─────────────────────────────────────────────────────┘
             ↑ implements
┌────────────┴────────────────────────────────────────┐
│ Infrastructure (External Concerns)                  │
│   - PrimeTweenService, UnityInputService            │
│   - AudioService, AdMobService (new)                │
│   - HapticFeedbackService (Android impl)            │
└─────────────────────────────────────────────────────┘
             ↑ composes
┌────────────┴────────────────────────────────────────┐
│ Composition (DI Root, LifetimeScope)                │
│   - GameInstaller : VContainer.LifetimeScope        │
│   - Wires all interfaces to implementations         │
└─────────────────────────────────────────────────────┘
```

### 1.2 Asmdef Bağımlılık Grafiği

```
PuzzleGame.Domain       → (no refs)
PuzzleGame.Application  → [Domain]
PuzzleGame.Infrastructure → [Application, Domain]
PuzzleGame.Composition  → [Domain, Application, Infrastructure, VContainer, PrimeTween, TextMeshPro, Unity.InputSystem, URP]
PuzzleGame.Tests        → [Domain, Application, Infrastructure]
PuzzleGame.Editor       → [Domain, Application, Infrastructure, Composition]
```

**Kurallar:**
- Domain → asla başka katmana bağımlı değil (Unity-agnostic)
- Application → Domain interfaces kullanır, Infrastructure interface'lerini inject eder
- Infrastructure → Application interfaces implement eder
- Composition → Tüm katmanları bağlar (DI root)
- Cycle YOK (önceki turda Application↔Infrastructure cycle kırıldı)

## 2. DI Container (VContainer)

### 2.1 GameInstaller (Composition Root)

```csharp
public class GameInstaller : LifetimeScope
{
    [SerializeField] private MoldPoolInitializer _moldPool;
    [SerializeField] private CameraEffects _cameraEffects;
    [SerializeField] private ErrorIndicator _errorIndicator;
    
    protected override void Configure(IContainerBuilder builder)
    {
        // Domain
        builder.Register<OreSortSolver>(Lifetime.Singleton);
        builder.Register<ILocalizationService, LocalizationService>(Lifetime.Singleton);
        
        // Application
        builder.Register<ICoinWallet, CoinWallet>(Lifetime.Singleton);
        builder.Register<IHintService, HintService>(Lifetime.Singleton);
        builder.Register<IUndoService, UndoService>(Lifetime.Singleton);
        builder.Register<ITutorialService, TutorialService>(Lifetime.Singleton);
        builder.Register<IDailyChallengeService, DailyChallengeService>(Lifetime.Singleton);
        builder.Register<IStreakService, StreakService>(Lifetime.Singleton);
        builder.Register<IHapticFeedbackService, HapticFeedbackService>(Lifetime.Singleton);
        builder.Register<IAnalyticsService, NoOpAnalyticsService>(Lifetime.Singleton);
        builder.Register<IAdService, NoOpAdService>(Lifetime.Singleton);
        builder.Register<IAgeVerificationService, AgeGateService>(Lifetime.Singleton);
        builder.Register<IConsentManager, ConsentManager>(Lifetime.Singleton);
        builder.Register<IScreenTransitionService, ScreenTransitionService>(Lifetime.Singleton);
        
        // Configuration
        builder.RegisterInstance(_economyConfig);
        builder.RegisterInstance(_uiStyleConfig);
        
        // Infrastructure
        builder.Register<ITweenService, PrimeTweenService>(Lifetime.Singleton);
        builder.Register<IAudioService, AudioService>(Lifetime.Singleton);
        builder.Register<ICrashReportingService, SentryService>(Lifetime.Singleton);
        
        // Presentation components (auto-inject)
        builder.RegisterComponentInHierarchy<MoldPoolInitializer>();
        builder.RegisterComponentInHierarchy<CameraEffects>();
        builder.RegisterComponentInHierarchy<ErrorIndicator>();
    }
}
```

### 2.2 Injection Patterns

- **POCO services:** Constructor injection (primary)
- **MonoBehaviours:** `[Inject]` field (only for components in scene)
- **Configuration:** `RegisterInstance` (ScriptableObject)
- **EventAggregator:** Singleton (auto-injected)

## 3. Mevcut Sistem Envanteri

### 3.1 Domain Katmanı (Pure C#)

| Dosya | LOC | Sorumluluk |
|---|---|---|
| `Models/LevelData.cs` | 110 | Level config (ScriptableObject) |
| `Models/MoldState.cs` | ~60 | Mold durumu (layer, isEmpty) |
| `Models/OreLayer.cs` | ~30 | Ore layer (color, amount) |
| `Models/DomainColor.cs` | ~40 | Renk struct (R,G,B) |
| `Models/LocalizationEntry.cs` | ~50 | Localization key enum |
| `Services/LocalizationService.cs` | 357 | 56 key × 5 dil |
| `Services/OreSortSolver.cs` | 276 | Hint + solvability kontrolü |
| `Services/MoldValidationService.cs` | ~100 | Cast kuralları |
| `Services/DifficultyBasedLevelGenerator.cs` | ~150 | Seviye üretici (retry loop) |
| `Interfaces/ILevelGenerator.cs` | ~30 | GenerateSolvable kontratı |
| `Interfaces/ITweenService.cs` | ~60 | Tween abstraction |
| `Interfaces/IMoldView.cs` | ~40 | Mold render kontratı |

**Toplam:** ~1.300 LOC (tahmini)

### 3.2 Application Katmanı

| Dosya | LOC | Sorumluluk |
|---|---|---|
| `Services/CastService.cs` | ~120 | Cast akışı (select, validate, execute) |
| `Services/GameSaveManager.cs` | 326 | PlayerPrefs persistence |
| `Services/CoinWallet.cs` | ~100 | Coin bakiye (PlayerPrefs) |
| `Services/HintService.cs` | ~80 | Hint (OreSortSolver wrap) |
| `Services/UndoService.cs` | ~80 | Undo (state history) |
| `Services/TutorialService.cs` | ~180 | 6-step onboarding |
| `Services/DailyChallengeService.cs` | ~120 | UTC-deterministic seed |
| `Services/StreakService.cs` | ~100 | Günlük streak tracking |
| `Services/HapticFeedbackService.cs` | ~60 | Mobile-native haptic |
| `Services/NoOpAnalyticsService.cs` | ~80 | 18-event taxonomy |
| `Services/ScreenTransitionService.cs` | ~80 | Fade in/out |
| `Services/LevelSetupService.cs` | ~150 | Level → Scene setup |
| `Services/AnimationService.cs` | 293 | Animation orchestration |
| `Services/InputHandlerService.cs` | 261 | Input event aggregation |
| `Services/AgeGateService.cs` | 80 | COPPA age gate (PII-safe: year+month) |
| `Configuration/EconomyConfig.cs` | 26 | Coin, cost, reward |
| `Configuration/UIStyleConfig.cs` | ~50 | UI style values |
| `Events/*` | ~200 | EventAggregator messages |
| `Interfaces/IAdService.cs` | 38 | Rewarded/Interstitial/Consent kontratı |
| `Interfaces/IConsentManager.cs` | 16 | GDPR consent kontratı |
| `Interfaces/IAgeVerificationService.cs` | 15 | COPPA age gate kontratı |

**Toplam:** ~2.500 LOC (tahmini)

### 3.3 Infrastructure Katmanı

| Dosya | LOC | Sorumluluk |
|---|---|---|
| `Implementations/PrimeTweenService.cs` | ~200 | PrimeTween wrapper |
| `Implementations/PourSystemController.cs` | 335 | Pour animation logic |
| `Implementations/AudioService.cs` | ~80 | AudioSource orchestration |
| `Implementations/HapticFeedbackService.cs` | ~60 | Android Vibrator (AndroidJNI) |
| `Implementations/UnityInputService.cs` | (NEW) | Input System wrap |
| `Implementations/FirebaseAnalyticsService.cs` | 110 | Firebase SDK wrap (`#if HAS_FIREBASE_ANALYTICS`) |
| `Implementations/AdMobService.cs` | 230 | AdMob SDK wrap (`#if HAS_GOOGLE_MOBILE_ADS`) |
| `Implementations/NoOpAdService.cs` | 65 | Test/CI fallback |
| `Implementations/ConsentManager.cs` | 123 | UMP SDK wrap (`#if HAS_GOOGLE_MOBILE_ADS`) |

**Toplam:** ~800 LOC (tahmini)

### 3.4 Presentation Katmanı

| Dosya | LOC | Sorumluluk |
|---|---|---|
| `GameManager.cs` | 136 | DI failure, FPS, audio boot, cleanup, onboarding trigger |
| `LevelFlowController.cs` | ~120 | Level load lifecycle |
| `WinLoseEvaluator.cs` | ~100 | Win/lose detection |
| `MoldController.cs` | ~260 (refactored from 434) | IMoldView facade; composes 3 POCOs |
| `MoldStateManager.cs` | ~107 (NEW) | MoldState lifecycle, serialized layer data |
| `MoldVisualSync.cs` | ~144 (NEW) | Visual layer list + cast progress math |
| `MoldAnimator.cs` | ~64 (NEW) | Completion flash, settle bounce, wobble |
| `MoldPoolInitializer.cs` | ~150 | Object pool, IActiveMoldsProvider |
| `OnboardingFlowController.cs` | 95 | POCO orchestrator: Splash → AgeGate → Consent → MainMenu |
| `UI/HudPresenter.cs` | ~200 | HUD binding (stars, coins, moves) |
| `UI/DebugOverlayUI.cs` | ~120 | Developer debug overlay |
| `UI/AgeGateModal.cs` | 80 | First-launch DOB picker (year+month sliders) |
| `UI/ConsentModal.cs` | 110 | GDPR consent dialog (Accept/Reject/Manage) |
| `UI/SettingsPrivacyController.cs` | 100 | Settings > Privacy toggles, reset, delete data |
| `UI/MainMenuController.cs` | ~190 | Main menu (Play/Daily/Settings/Privacy buttons, coin/streak display, sub-panel nav) |
| `UI/LevelBiomeClassifier.cs` | 34 | Biome enum + L01-25/L26-50 boundary POCO |
| `UI/BiomeProgress.cs` | ~60 | Per-biome completion/star count POCO (testable) |
| `UI/WorldMapController.cs` | ~165 | 2 biome cards (CrystalMines/VolcanicForge) with progress + click→filtered LevelSelect |
| `UI/BiomeCardView.cs` | (in WorldMapController.cs) | Individual biome card component |
| `UI/DailyChallengeController.cs` | ~170 | Daily challenge entry screen (streak, countdown, play, back) |
| `UI/DailyChallengeCountdown.cs` | ~45 | UTC midnight reset time + HH:MM:SS formatter POCO |

**Application/Configuration (art):**

| Dosya | LOC | Sorumluluk |
|---|---|---|
| `BiomeArtCatalog.cs` | ~35 | ScriptableObject catalog of AI-generated biome art (sprites, accent colors). Populated in Editor via drag-and-drop from Midjourney/DALL-E outputs. |

**Application/Interfaces (art):**

| Dosya | LOC | Sorumluluk |
|---|---|---|
| `IBiomeArtProvider.cs` | ~20 | Art provider contract (card bg, mold bg, icon, accent color by biome) |

**Infrastructure/Implementations (art):**

| Dosya | LOC | Sorumluluk |
|---|---|---|
| `ScriptableObjectBiomeArtProvider.cs` | ~35 | Reads BiomeArtCatalog; graceful fallback (null/white) when catalog empty — supports soft-launch-without-art |
| `CameraEffects.cs` | ~80 | Camera shake/zoom |

**Toplam:** ~1.700 LOC

### 3.5 Composition Katmanı

| Dosya | LOC | Sorumluluk |
|---|---|---|
| `GameInstaller.cs` | ~150 | DI registration |
| `EntryPoint.cs` | ~30 | Bootstrapping |

**Toplam:** ~180 LOC

### 3.6 Editor Katmanı

| Dosya | LOC | Sorumluluk |
|---|---|---|
| `SceneBuilder.cs` | 613 | Editor scene builder |
| `LevelsTab.cs` | 543 | Level editor tab |
| `TestTab.cs` | 538 | Test runner tab |
| `LocalizationTab.cs` | 435 | Localization editor |
| `LevelUITab.cs` | 320 | Level UI editor |
| `PaletteTab.cs` | 291 | Color palette editor |
| `SceneTab.cs` | 289 | Scene settings editor |
| `FeaturesTab.cs` | 268 | Feature flags editor |

**Toplam:** ~3.300 LOC

### 3.7 Tests Katmanı

| Dosya | LOC | Sorumluluk |
|---|---|---|
| `Domain/Services/LevelGeneratorSolvabilityTests.cs` | ~200 | 8 tests |
| `Application/Services/CoinWalletTests.cs` | ~150 | 11 tests |
| `Application/Services/TutorialServiceTests.cs` | ~200 | 11 tests |
| `Application/Services/AgeGateServiceTests.cs` | ~120 | 7 tests (COPPA, PII) |
| `Infrastructure/NoOpAdServiceTests.cs` | ~90 | 7 tests (test fallback) |
| `Infrastructure/ConsentManagerTests.cs` | ~80 | 6 tests (state transitions) |
| `Infrastructure/OnboardingFlowLogicTests.cs` | ~120 | 4 tests (under-13, adult, reject, reset) |
| `Presentation/LevelBiomeClassifierTests.cs` | ~60 | 7 tests (biome boundary, invalid levels) |
| `Presentation/BiomeProgressTests.cs` | ~170 | 17 tests (count/star/sum/complete per biome) |
| `Presentation/DailyChallengeCountdownTests.cs` | ~120 | 12 tests (UTC midnight rollover, month/year boundary, format) |
| `Presentation/ErrorIndicatorBootstrapTests.cs` | ~75 | 5 tests (API contract + auto-created name; 2 PlayMode tests for actual FindOrCreate) |
| `Application/BiomeArtCatalogTests.cs` | ~95 | 8 tests (null/empty/single/multi/null-entry-skipped/no-match/accent color) |
| `Fakes/Fake*.cs` | ~300 | 6 fake classes |

**Toplam:** ~103 tests, ~1.780 LOC

**Grand Total:** ~10.000 LOC

## 4. 3rd Party Entegrasyon Noktaları

### 4.1 Mevcut Paketler (`Packages/manifest.json`)

| Paket | Versiyon | Kullanım |
|---|---|---|
| `com.unity.render-pipelines.universal` | 17.4.0 | URP |
| `com.unity.inputsystem` | 1.19.0 | Input System |
| `com.unity.ugui` | 2.0.0 | UI Toolkit + uGUI |
| `com.unity.visualeffectgraph` | 17.4.0 | ❌ Kaldırılacak (kullanılmıyor) |
| `com.unity.timeline` | 1.8.12 | ❌ Kaldırılacak (kullanılmıyor) |
| `com.unity.visualscripting` | 1.9.11 | ❌ Kaldırılacak (kullanılmıyor) |
| `com.unity.ai.navigation` | 2.0.13 | ❌ Kaldırılacak (kullanılmıyor) |
| `com.unity.multiplayer.center` | 1.0.1 | ❌ Kaldırılacak (multiplayer yok) |
| `com.unity.test-framework` | 1.6.0 | NUnit tests |
| `com.unity.testtools.codecoverage` | 1.3.0 | Code coverage |
| `jp.hadashikick.vcontainer` | 1.18.0 | DI |
| `com.kyrylokuzyk.primetween` | 1.4.0 | Tween |

**Yeni eklenecek (Hafta 2-3):**
- `com.google.ads.mobile` 9.0.0 (AdMob, UMP consent dahil)
- Firebase Unity SDK (Analytics only)

### 4.2 Build Pipeline

**`Assets/Plugins/Android/AndroidManifest.xml`:**
```xml
<manifest xmlns:android="http://schemas.android.com/apk/res/android"
          package="com.oresorter.app"
          xmlns:tools="http://schemas.android.com/tools">
  <application>
    <!-- AdMob App ID -->
    <meta-data
        android:name="com.google.android.gms.ads.APPLICATION_ID"
        android:value="ca-app-pub-XXXXX~XXXXX" />
  </application>
</manifest>
```

**Build target settings:**
- Scripting Backend: IL2CPP
- Target Architectures: ARM64
- Min SDK: 24
- Target SDK: 34
- Graphics API: Vulkan
- Color Space: Linear

## 5. EventAggregator Pattern

### 5.1 Event Tanımları

```csharp
// Application/Events/LevelEvents.cs
public class LevelSelectedEvent { public int LevelNumber; }
public class LevelLoadedEvent { public LevelData Level; }
public class CastCompletedEvent { public int MovesUsed; }
public class LevelWonEvent { public int Stars, MovesUsed; }
public class LevelLostEvent { public int MovesUsed; }
public class CoinsChangedEvent { public int NewBalance; }
```

### 5.2 Subscription Pattern

```csharp
public class HudPresenter : MonoBehaviour, IDisposable
{
    [Inject] private IEventAggregator _events;
    
    private void OnEnable()
    {
        _events.Subscribe<LevelLoadedEvent>(OnLevelLoaded);
        _events.Subscribe<LevelWonEvent>(OnLevelWon);
        _events.Subscribe<CoinsChangedEvent>(OnCoinsChanged);
    }
    
    public void Dispose()
    {
        _events.Unsubscribe<LevelLoadedEvent>(OnLevelLoaded);
        _events.Unsubscribe<LevelWonEvent>(OnLevelWon);
        _events.Unsubscribe<CoinsChangedEvent>(OnCoinsChanged);
    }
    
    private void OnDestroy() => Dispose();
}
```

**Memory leak önleme:** Her subscriber `IDisposable` implement eder, `OnDestroy` veya explicit `Dispose` çağrısında unsubscribe eder.

## 6. Performance Considerations

### 6.1 Frame Budget (60 FPS = 16.6ms/frame)

| Sistem | Budget | Strateji |
|---|---|---|
| Render (URP) | 8ms | Mobile URP asset, MSAA off, simple shaders |
| Cast animation | 2ms | PrimeTween (zero-alloc) |
| Input | 1ms | EventAggregator (zero-alloc pub/sub) |
| UI update (HUD) | 1ms | TMP text pooling, minimal redraw |
| AI (OreSortSolver) | 0.5ms | Hint sadece talepte çalışır |
| Other | 4.1ms | Reserve |

### 6.2 Memory Budget

| Kategori | Limit |
|---|---|
| Total | 200MB (low-end device) |
| Textures | 50MB (compressed) |
| Audio | 20MB (OGG, streaming) |
| Mesh | 10MB |
| Code (IL2CPP) | 30MB |
| Heap | 50MB (GC.Collect periodically) |
| Other | 40MB |

### 6.3 Allocation Hotspots (Refactor Hedefleri)

1. **MoldController.cs (386 LOC):** MonoBehaviour IMoldView, 3 service injection, animation+input+validation → 3 class'a böl
2. **HudPresenter Update:** TMP text update her frame değil, sadece değişince
3. **DebugOverlayUI:** StringBuilder reuse, per-frame allocation yok
4. **InputHandlerService:** 261 LOC → 2-3 service'e böl (Input + Cast input + UI input)

## 7. Test Stratejisi (Detay)

### 7.1 EditMode Tests (NUnit, hızlı)

- Domain: OreSortSolver, LevelGenerator, MoldValidation
- Application: CoinWallet, HintService, UndoService, TutorialService, DailyChallengeService, StreakService
- Toplam: 30+ test (mevcut 28 + yeni 2-5)

### 7.2 PlayMode Tests (UnityTest, yavaş)

- Tutorial 6-adım smoke test
- L01 → L02 full win flow
- Daily challenge: 7 gün × 3 cihaz aynı seed
- AdMob mock integration
- Crash reporting: deliberately throw, verify capture

### 7.3 CI Workflow (`.github/workflows/ci.yml`)

- EditMode tests
- PlayMode tests (headless)
- Android IL2CPP build (verification)
- Lint (C# conventions, .editorconfig)
- Code coverage report

## 8. Bilinen Teknik Borçlar

| Borç | Severity | Aksiyon |
|---|---|---|
| MoldController.cs 434 LOC (god class) | Major | ✅ **Sprint #2 tamamlandı:** 3 POCO çıkarıldı (MoldStateManager, MoldVisualSync, MoldAnimator); controller ~260 LOC facade'e slimlendi |
| MainMenu + Level Select navigation flat (no World Map) | Major | ✅ **Sprint #4 tamamlandı:** WorldMapController + BiomeProgress POCO + BiomeCardView; 2 biome kartı yan yana, biome-filtered LevelSelect, progress tracking |
| Daily Challenge UI yok (stub) | Major | ✅ **Sprint #5 tamamlandı:** DailyChallengeController + DailyChallengeCountdown POCO; entry screen with streak/longest-streak/countdown, UTC midnight reset, DailyChallengeStartedEvent for level seed handoff |
| AI art integration infrastructure yok | Major | ✅ **Sprint #6 tamamlandı:** IBiomeArtProvider interface (Application) + BiomeArtCatalog ScriptableObject + ScriptableObjectBiomeArtProvider impl (Infrastructure); Biome enum refactored to Domain.Models for cross-layer accessibility; provider gracefully returns defaults when catalog empty (soft-launch-friendly) |
| VContainer crash on misconfigured scenes (ErrorIndicatorController missing) | Major | ✅ **Sprint #7 tamamlandı:** ErrorIndicatorBootstrap static helper auto-creates ErrorIndicatorController if missing from scene (replaces RegisterComponentInHierarchy with RegisterInstance + EnsureExists). Unity 6 modern API (FindAnyObjectByType), DontDestroyOnLoad, warns on auto-create. |
| Editor tooling şişman (SceneBuilder 613 LOC) | Minor | MVVM refactor (opsiyonel) |
| PourSystemController.cs 335 LOC | Minor | Interface segregation |
| LocalizationService 357 LOC (inline data) | Minor | JSON externalization (runtime load) |
| InputHandlerService 261 LOC | Minor | 2-3 service'e böl |
| DebugOverlayUI string allocation | Minor | StringBuilder reuse |
| EventAggregator memory leak riski | Medium | PlayMode test 100-level sim |
| No memory profiling baseline | Major | Memory Profiler integration |

## 9. Güvenlik Notları

### 9.1 Save Data

- PlayerPrefs (plain text) → kötü amaçlı root'lu cihazda değiştirilebilir
- v1.0: kabul edilebilir risk (sadece coin, level progress)
- v1.1: HMAC-signed save (server olmadan bile tamper-evident)
- v1.2: Cloud save (Google Play Games)

### 9.2 IAP

- v1.0: IAP yok → risk sıfır
- İleride: Unity IAP veya RevenueCat (ücretsiz tier, %1-2 fee)

### 9.3 Network

- Tüm 3rd party çağrılar TLS (Firebase, AdMob, UMP default)
- Local save asla network'e gönderilmez

## 10. Yeni Sistemler (Eklenecek)

### 10.1 AdMobService (Hafta 2)
- Interface: `IAdService`
- Implementation: `AdMobService.cs` (Infrastructure)
- Methods: Initialize, ShowRewarded, ShowInterstitial, SetPersonalized
- 3rd party: `com.google.ads.mobile` 9.0.0

### 10.2 FirebaseAnalyticsService (Hafta 3)
- Interface: `IAnalyticsService` (mevcut)
- Implementation: `FirebaseAnalyticsService.cs` (Infrastructure)
- Firebase Unity SDK (Analytics only, ~500KB)
- 18 event taxonomy zaten NoOp'ta tanımlı

### 10.3 SentryService
**Karar: Çıkarıldı (kullanıcı tercihi).** Sentry Unity SDK'sı v1.0 kapsamında entegre edilmiyor. Crash reporting, native Android `Logcat` ve Firebase Analytics'in `ErrorShown` event'i üzerinden izlenecek. İleride gerekirse 3rd party SDK'sız in-house çözüm değerlendirilir.

### 10.4 AgeGateService (Hafta 3)
- Interface: `IAgeVerificationService` (yeni)
- Implementation: `AgeGateService.cs` (Application)
- DateTime.UtcNow + saved birth year → isUnder13 bool
- Settings > Privacy > Re-verify

### 10.5 ConsentManager (Hafta 3)
- Interface: `IConsentManager` (yeni)
- Implementation: `ConsentManager.cs` (Infrastructure)
- Google UMP SDK
- Methods: ShowConsentIfNeeded, IsConsentGiven, ResetConsent

## 11. Deployment Pipeline

### 11.1 Build Targets

| Build | Scripting | Architecture | Size | Store |
|---|---|---|---|---|
| Development | Mono | x86_64 (editor) | — | Local |
| Android Debug | IL2CPP | ARM64 | 80MB | Test |
| Android Release | IL2CPP | ARM64 | 60MB | Google Play |
| Android AAB | IL2CPP | ARM64 | 50MB (compressed) | Google Play |

### 11.2 Release Checklist

- [ ] Version bump (PlayerSettings.bundleVersion)
- [ ] Build number increment
- [ ] ProGuard/R8 config (release builds)
- [ ] AdMob production ID
- [ ] Firebase production config
- [ ] Privacy Policy URL
- [ ] Terms of Service URL
- [ ] App icon (tüm boyutlar)
- [ ] Feature graphic (1024×500)
- [ ] Screenshots (min 4, max 8)
- [ ] Short description (80 char)
- [ ] Full description (4000 char)
- [ ] What's new (release notes)
- [ ] Content rating (IARC questionnaire)
- [ ] Data safety form
- [ ] Target audience (NOT children)
- [ ] Pricing (Free)
- [ ] Distribution (All countries except China, Russia, Iran)
- [ ] Internal testing track
- [ ] Closed alpha/beta track (opsiyonel)
- [ ] Production track
