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
    [SerializeField] private CameraEffectsController _cameraEffects;
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
        builder.RegisterComponentInHierarchy<CameraEffectsController>();
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
| `UI/SettingsSoundController.cs` | ~145 | Settings > Sound (BGM/SFX toggle + volume slider, persisted via IAudioSettingsService) |
| `UI/MainMenuController.cs` | ~270 | Main menu (Play/Daily/Settings/Privacy/Sound buttons, coin/streak display, sub-panel nav, fade-in on enter, fade-out on sub-panel open via ITweenService+CanvasGroup) |
| `UI/LevelBiomeClassifier.cs` | 34 | Biome enum + L01-25/L26-50 boundary POCO |
| `UI/BiomeProgress.cs` | ~60 | Per-biome completion/star count POCO (testable) |
| `UI/WorldMapController.cs` | ~165 | 2 biome cards (CrystalMines/VolcanicForge) with progress + click→filtered LevelSelect |
| `UI/BiomeCardView.cs` | (in WorldMapController.cs) | Individual biome card component |
| `UI/DailyChallengeController.cs` | ~170 | Daily challenge entry screen (streak, countdown, play, back) |
| `UI/DailyChallengeCountdown.cs` | ~45 | UTC midnight reset time + HH:MM:SS formatter POCO |
| `ErrorIndicatorBootstrap.cs` | ~30 | Static FindOrCreate helper — ensures ErrorIndicatorController exists in scene (auto-creates with warning if missing); used by GameInstaller to avoid VContainer crash on misconfigured scenes |

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

**Application/Interfaces (audio):**

| Dosya | LOC | Sorumluluk |
|---|---|---|
| `IAudioSettingsService.cs` | ~85 | Persistent audio preferences contract + AudioPreferences readonly struct (MusicEnabled/SfxEnabled/MusicVolume/SfxVolume, default 0.6/0.8, clamps 0-1, ==/!= operators). Named "AudioPreferences" (not "AudioSettings") to avoid collision with UnityEngine.AudioSettings. |

**Infrastructure/Implementations (audio):**

| Dosya | LOC | Sorumluluk |
|---|---|---|
| `PlayerPrefsAudioSettingsService.cs` | ~95 | PlayerPrefs-backed persistence (PuzzleGame.Audio.* keys), single source of truth, raises AudioSettingsChangedEvent on change. Bridge: C# event → EventAggregator via optional ctor param. |

**Toplam:** ~1.950 LOC

### 3.5 Composition Katmanı

| Dosya | LOC | Sorumluluk |
|---|---|---|
| `GameInstaller.cs` | ~150 | DI registration |
| `EntryPoint.cs` | ~30 | Bootstrapping |

**Toplam:** ~180 LOC

### 3.6 Editor Katmanı

| Dosya | LOC | Sorumluluk |
|---|---|---|
| `SceneBuilder.cs` + `SceneBuilderModel.cs` + `SceneBuilderPrimitives.cs` + `SceneBuilderMoldFactory.cs` | ~760 (4 dosya) | Editor scene builder (orchestrator + data + primitives + mold factory, Sprint #14) |
| `LevelsTab.cs` | 543 | Level editor tab (Solution Playback Engine entegrasyonu: OreSortSolver çözümlerini sahne üzerinde oynatma/adım adım izleme) |
| `TestTab.cs` | 538 | Test runner tab |
| `LocalizationTab.cs` | 435 | Localization editor (JSON serialization & DTO sync) |
| `LevelUITab.cs` | 320 | Level UI editor |
| `PaletteTab.cs` | 291 | Color palette editor |
| `SceneTab.cs` | 289 | Scene settings editor (Visual Scene View Painter entegrasyonu: Scene View içinde doğrudan katman ekleme/çıkarma/temizleme) |
| `FeaturesTab.cs` | 268 | Feature flags editor |
| `LevelDataBatchCreator.cs` | ~195 | GDD-aligned 50-level batch creator (testable POCO `GetParametersForLevel(int)` + skip-existing asset logic) |

**Temel Editor Araçları ve Yeni Özellikler:**
1. **Visual Scene View Painter (`SceneTab.cs`):** Editörlerin/tasarımcıların Unity Scene View içerisinde doğrudan mold'ları boyamasını sağlar. Ekran alanında yüzen renk paleti, katman ekleme (push), katman geri alma (pop) ve tüm katmanları temizleme (clear) butonları sunar.
2. **Solution Playback Engine (`LevelsTab.cs`):** Bölümlerin çözülebilirliğini test etmek için `OreSortSolver` tarafından üretilen çözüm adımlarını sahne üzerinde adım adım (Play, Step Forward, Step Backward, Stop) oynatır ve animasyonlu olarak doğrulamayı sağlar.

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
| `Application/AudioPreferencesTests.cs` | ~85 | 7 tests (defaults, EffectiveMusicVolume/SfxVolume gating, clamp, With builders, ==/!= equality, hash) |
| `Infrastructure/PlayerPrefsAudioSettingsServiceTests.cs` | ~135 | 7 tests (load defaults, load persisted, setters+persist+event, no-op skip, reset, save, null eventAggregator) |
| `Editor/LevelDataBatchCreatorTests.cs` | ~90 | 9 tests (out-of-range, all-50-non-default, tier escalation Trivial→Expert, biome distribution 25/25, biome seam matches LevelBiomeClassifier, MoldCount bounds, ColorCount ramp + max, unique seed per level, ParMoves/GoodMoves thresholds) |
| `Application/MemorySnapshotDiffTests.cs` | ~140 | 9 tests (Normal/Warning/Critical verdict thresholds, negative delta, all-metric deltas, allocated-vs-gc-delta) |
| `Infrastructure/UnityMemorySnapshotServiceTests.cs` | ~85 | 5 tests (timestamp range, allocated ≤ reserved invariant, non-negative values, identical snapshots → Normal, huge delta → Critical) |
| `Application/EventAggregatorMemoryTests.cs` | ~140 | 5 tests (unsubscribe clears, publish-after-unsubscribe no-op, 100-cycle memory growth ≤ 5 MB, 10-distinct-types cleanup, Clear() resets dict) |
| `Infrastructure/PourSystemControllerInterfaceSegregationTests.cs` | ~125 | 11 tests (IPourSystemController inherits 3 focused interfaces, controller implements all 4, focused interfaces are distinct contracts, each interface exposes only its own methods via reflection) |
| `Fakes/Fake*.cs` | ~300 | 6 fake classes |

**Toplam:** ~166 tests, ~2.715 LOC (Sprint #13 + #14 refactor-only — yeni test eklenmedi; Sprint #14'te 710 → ~760 LOC ama 4 focused dosyaya yayıldı, public API preserved)

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
| MoldController.cs editor-time refresh reflection dependency | Medium | ✅ **Sprint #15/16 tamamlandı:** Editör araçlarında (`LevelsTab` ve `SceneTab`) `MoldController`'ın editor-time görsel yenilemesi için kullanılan `RestoreStateFromSerialized` metodu public yapılarak reflection (GetMethod) bağımlılığı tamamen kaldırıldı ve doğrudan/tip güvenli çağrılara dönüştürüldü. |
| L11-L50 level data eksik (L01-L10 hand-tuned, GDD 50-level campaign için batch tool yoktu) | Major | ✅ **Sprint #1 tamamlandı:** LevelDataBatchCreator refactored to 50 levels (GDD-aligned 5-tier progression: L01-10 Trivial / L11-20 Easy / L21-30 Medium / L31-40 Hard / L41-50 Expert), biome-aware via `GetParametersForLevel(int)` static POCO that uses `LevelBiomeClassifier` (L01-25 CrystalMines, L26-50 VolcanicForge), intra-tier color ramp (every 2 levels +1, capped at MaxColorsPerLevel), seed formula `levelNumber * 1337` for unique per-level determinism. `CreateAllLevels()` skips existing assets (preserves L01-L10 hand-tuned). LevelsTab button label updated to "Create 50 Levels (GDD-aligned)". `autoGenerate = true` — runtime `DifficultyBasedLevelGenerator` populates Molds deterministically. |
| MainMenu + Level Select navigation flat (no World Map) | Major | ✅ **Sprint #4 tamamlandı:** WorldMapController + BiomeProgress POCO + BiomeCardView; 2 biome kartı yan yana, biome-filtered LevelSelect, progress tracking |
| Daily Challenge UI yok (stub) | Major | ✅ **Sprint #5 tamamlandı:** DailyChallengeController + DailyChallengeCountdown POCO; entry screen with streak/longest-streak/countdown, UTC midnight reset, DailyChallengeStartedEvent for level seed handoff |
| AI art integration infrastructure yok | Major | ✅ **Sprint #6 tamamlandı:** IBiomeArtProvider interface (Application) + BiomeArtCatalog ScriptableObject + ScriptableObjectBiomeArtProvider impl (Infrastructure); Biome enum refactored to Domain.Models for cross-layer accessibility; provider gracefully returns defaults when catalog empty (soft-launch-friendly) |
| VContainer crash on misconfigured scenes (ErrorIndicatorController missing) | Major | ✅ **Sprint #7 tamamlandı:** ErrorIndicatorBootstrap static helper auto-creates ErrorIndicatorController if missing from scene (replaces RegisterComponentInHierarchy with RegisterInstance + EnsureExists). Unity 6 modern API (FindAnyObjectByType), DontDestroyOnLoad, warns on auto-create. |
| Audio settings UI yok (volume/toggle exposed ama persist + player-facing yok) | Major | ✅ **Sprint #8 tamamlandı:** IAudioSettingsService (Application) + AudioPreferences readonly struct (player prefs POCO, NOT named "AudioSettings" to avoid UnityEngine.AudioSettings collision) + PlayerPrefsAudioSettingsService (Infrastructure, persistent via PlayerPrefs) + AudioSettingsChangedEvent (EventAggregator bridge) + SettingsSoundController (Presentation, separate sub-panel) + MainMenuController'a Sound button + CanvasGroup fade-in on enter + ITweenService fade-out when sub-panel opens. |
| CameraEffects.cs doc/code sample mismatch (actual: CameraEffectsController.cs) | Minor | ✅ Sprint #7+#8 fix: 2 code samples in Section 3 updated to reference CameraEffectsController. |
| Editor tooling şişman (SceneBuilder 613 LOC) | Minor | ✅ **Sprint #14 tamamlandı:** 710 LOC god-class 4 focused dosyaya bölündü — `SceneBuilderModel` (data types: `BuildOptions`/`MoldConfig`/`MoldLayout`/`ShaderVariant`/`DefaultPalette` + tüm color/vector sabitleri, ~120 LOC, public static), `SceneBuilderPrimitives` (lighting/ground/camera/post-processing/cauldron + dust/fire particles + 4 material preset + `CreateLitMaterial`/`CreatePrimitive`/`FindShader` helpers, ~280 LOC, internal static), `SceneBuilderMoldFactory` (`CreateMold`/`CreateDefaultMoldSet`/`RemoveMolds`/`CountMolds`/`ComputePositions`/`GenerateMixedContents`/`BuildLayers`/`GetUniqueName`, ~180 LOC, internal static), `SceneBuilder` (slim orchestrator ~180 LOC, public API preserved via delegation to MoldFactory+Primitives). `using static` → 4 ayrı `using X = ...` alias (`UnityEditor.BuildOptions` ile çakışma çözümü). 4 tüketici dosyası (`SceneTab`/`LevelSolverUtility`/`LevelUITab`/`LevelsTab`) `SceneBuilder.X` type/constant referansları `SceneBuilderModel.X`'e güncellendi (method çağrıları `SceneBuilder.X()` korundu). `Assembly-CSharp-Editor.csproj` redundant `<Compile Include>` entry'si temizlendi. **Net etki:** 710 → ~760 LOC toplam (4 dosyaya yayılmış, her biri tek sorumluluk), public API surface korundu (sadece type erişimi için `SceneBuilderModel` namespace prefix gerekiyor — açık refactor). |
| PourSystemController.cs 335 LOC | Minor | ✅ **Sprint #10 tamamlandı:** Interface segregation — 3 focused Application interfaces (`IPourSimulator` gameplay preview+execute, `IPourHistoryService` undo snapshot+restore, `IPourDebugController` dev tools: mutators+overrides+queries+flags). `IPourSystemController` artık marker facade (3'ünü inherit eder, geriye uyumluluk). GameInstaller 4 interface'i de `.As<>()` ile expose ediyor (consumers en küçük contract'a bağlanır). Implementation (394 LOC) değişmedi — sadece contract bölündü. 11 segregation contract test. |
| LocalizationService 357 LOC (inline data) | Minor | ✅ **Sprint #12 tamamlandı:** JSON externalization (53 key × 5 lang → `Assets/StreamingAssets/Localization/translations.json`, UTF-8). Service 357 → 65 LOC slim. `JsonTranslationProvider` (Infrastructure) parses via `JsonUtility`. `HardcodedTranslationProvider` korundu OCP alternative olarak. Sprint #12 sonunda Android uyumluluğu TODO olarak işaretlendi (`Application.streamingAssetsPath` Android'de APK içinde → `File.ReadAllText` returns 0 bytes). **Sprint #15 tamamlandı (Android gap kapatıldı):** `IAsyncTranslationProvider` interface (`ITranslationProvider` extends + `Task LoadAsync(ct)` eklendi), `StreamingAssetsJsonTranslationProvider` (Infrastructure, `UnityWebRequest.Get` + polling loop, `Application.streamingAssetsPath` Android jar:// path için) + `LocalizationBootstrap` MonoBehaviour (Composition, try/catch guarded `IObjectResolver.Resolve<IAsyncTranslationProvider>` → `Start()` coroutine). `LocalizationService` ctor artık `provider.Load()` çağırmıyor — lazy load: `EnsureLoaded()` private method, first `GetString` veya `AddTranslation` çağrısında tetiklenir. GameInstaller Android platform guard: `#if UNITY_ANDROID && !UNITY_EDITOR` — `StreamingAssetsJsonTranslationProvider` 3 interface altında kayıt (concrete + `ITranslationProvider` + `IAsyncTranslationProvider`) + `LocalizationBootstrap` GameObject `RegisterComponent` ile. Editor/PC sync path (File.ReadAllText) korunur, Android async path bootstrap sırasında pre-load yapar, ilk `GetString` blocklanmaz. LocalizationServiceTests'a +3 test (ctor Load() çağırmaz, first GetString Load() tetikler, subsequent GetString'ler reload etmez) + `LoadTrackingProvider` helper. 0/0 build verified. |
| InputHandlerService 261 LOC | Minor | ✅ **Sprint #11 tamamlandı:** 3-service SRP split — `IMoldInputRouter` (input + selection + cast orchestration, ~210 LOC) + `IMoldLookupCache` (collider→mold cache, ~70 LOC, 0 dep) + `IInputHandlerDefaults` (play-test LevelData, ~45 LOC, 0 dep). Eski `IInputHandlerService` / `InputHandlerService` artık thin facade (3'ünü inherit eder + composes, ~75 LOC). GameInstaller 3 focused interface'i de `.Register<>` ile ayrı expose ediyor (MoldPoolInitializer sadece `IMoldLookupCache`, GameManager sadece `IMoldInputRouter` bağımlısı olabilir). Mevcut `InputHandlerServiceTests` 9 test SetUp'ta 3 service + facade construct edecek şekilde güncellendi — test contract yüzeyi (ProcessInput + SetMolds) korundu. |
| DebugOverlayUI string allocation | Minor | ✅ **Sprint #13 tamamlandı:** `DebugOverlayUI.RefreshDisplay()` artık tek `_sb` (StringBuilder 4096 capacity) üzerinde 3 bloğu (`_moldStateText` + `_serviceText` + `_vfxText`) sırayla `Clear() → Append() → ToString()` ile yazıyor. Önceki kod `_serviceText` ve `_vfxText` için string interpolation + `+` concatenation kullanıyordu → her frame 6+ string allocation. Yeni kod 3 ToString (TMP.text zorunlu, kaçınılmaz). 126 LOC, aynı davranış, GC baskısı yarıdan fazla azaldı. Test eklenmedi (mekanik refactor, görsel olarak Unity Editor'da doğrulanabilir). |
| EventAggregator memory leak riski | Medium | ✅ **Sprint #9 tamamlandı:** EventAggregatorMemoryTests — 100-cycle subscribe/publish/unsubscribe simülasyonu, 5 MB delta üst sınırı; 10 farklı event type ile dict stress; Clear() sonrası dict reset doğrulanıyor. IMemorySnapshotService ile entegre (harness/CLI üzerinden baseline + post-N-level diff). |
| No memory profiling baseline | Major | ✅ **Sprint #9 tamamlandı:** IMemorySnapshotService (Application) + MemorySnapshot readonly struct (TotalReserved/Allocated/Mono/GC bytes) + MemorySnapshotDiff + MemoryHealth enum (Normal/Warning/Critical) + UnityMemorySnapshotService impl (Infrastructure, `UnityEngine.Profiling.Profiler` + `GC.GetTotalMemory`, paket bağımsız). GameInstaller'a singleton olarak kayıtlı. ForgeConstants.MemoryWarningDeltaBytes (50 MB) + MemoryCriticalDeltaBytes (200 MB) threshold'ları verdict için. Daha derin snapshot ihtiyacında `com.unity.memoryprofiler` paketi sonradan drop-in impl olarak eklenebilir. |
| **YENİ** (Analiz 2026-06-06): Resources.Load sprawl (28 site) | Major | `AddressablesAssetProvider` (Infrastructure) Sprint #6'da kurulmuş ama 24/28 call site doğrudan `Resources.Load` çağırıyor, provider abstraction'ı bypass ediyor. **Runtime hot path: 11 site** (GameInstaller 6 Config asset, Wobble 1, StreamRenderer 1, ParticleFactory 3). **Editor tool: 11 site** (düşük öncelik). **Etki:** Startup -200-400ms, peak memory -50-100MB, APK -30-50% (content streaming), content update path açılır. **Sprint #16 önerildi.** |
| **YENİ** (Analiz 2026-06-06): 5 untested critical pure-logic services | Major | **Domain:** `DifficultyBasedLevelGenerator` (134 LOC, 50-level campaign üreticisi — determinism kritik). **Application:** `DailyChallengeService` (113 LOC, UTC seed + streak), `HintService` (90 LOC, coin economy), `UndoService` (57 LOC, snapshot stack), `AgeGateService` (57 LOC, GDPR/COPPA). Hepsi pure C# — test edilebilirlik yüksek. **Sprint #17 önerildi.** |
| **YENİ** (Analiz 2026-06-06): Heterogeneous async pattern (4 coroutine sites) | Minor | Coroutine (`StartCoroutine(IEnumerator)`) 4 call site: `ScreenTransitionService` (2), `ErrorIndicatorController` (2). Sprint #15 `Task<T>` async/await pattern'i kurmuşken (LocalizationBootstrap), diğer 4 site de UniTask/Task'a dönüştürülebilir. Cancellation token plumbing standardizasyonu + PlayMode test mocklanabilirlik. **Sprint #18 önerildi** (UniTask paket kararı kullanıcı onayı gerekir). |
| **YENİ** (Analiz 2026-06-06): GameSaveManager 326 LOC (4 karışık sorumluluk) | Minor | HMAC crypto + JSON serialization + File IO + in-memory cache tek dosyada. Sprint #18 (HMAC hardening) **iptal** — HMAC zaten var (analiz 2026-06-06 doğruladı: `GameSaveManager.cs:34-44` BuildSecretKey). Dosya refactor: 3 dosyaya böl (SaveCrypto + SaveStorage + GameSaveManager orchestrator). **Düşük öncelik, v1.0 sonrası.** |

## 9. Güvenlik Notları

### 9.1 Save Data

- `GameSaveManager` (Application) **HMAC-SHA256 anti-tamper zaten uygulanmış** (Sprint öncesi, doğrulama: `Assets\Scripts\Application\Services\GameSaveManager.cs:14-22` docstring + `:34-44` BuildSecretKey + `using System.Security.Cryptography;`). Salt = `SystemInfo.deviceUniqueIdentifier` + device model + constant pepper. **Sprint #18'de planlanan HMAC hardening iptal — bu özellik zaten v1.0'da mevcut.**
- Tamper tespit edildiğinde save reddedilir + loglanır (silent recovery; oyuncu yeni save başlatır)
- v1.2: Cloud save (Google Play Games) — v1.0 sonrası değerlendirilir

### 9.2 IAP

- v1.0: IAP yok → risk sıfır
- İleride: Unity IAP veya RevenueCat (ücretsiz tier, %1-2 fee)

### 9.3 Network

- Tüm 3rd party çağrılar TLS (Firebase, AdMob, UMP default)
- Local save asla network'e gönderilmez

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
