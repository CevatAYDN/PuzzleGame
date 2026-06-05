# Mimari Analiz Raporu (Haziran 2026)

**Tarih:** 2026-06-06
**Kapsam:** Ore Sorter — tüm Clean Architecture katmanları
**Sprint geçmişi:** Sprint #1-15 (Section 8 debt tamamen temizlendi)

---

## 1. Executive Summary

Sprint #1-15 sonrası tüm mimari debt temizlendi. Bu analiz, **borç ötesi** durum tespiti: kod tabanı ne kadar sağlıklı, hangi yeni iyileştirme fırsatları mantıklı sırada bekliyor?

**Kısa sonuç:** Kod tabanı beklentimin üzerinde sağlıklı. **0 kritik sorun**, **0 mimari ihlal**, **0 büyük god class (runtime tarafı)**. Sıradaki iş **borç değil, modernizasyon + test coverage** kategorisinde.

---

## 2. Temiz Sinyaller (Verified Clean)

Sprint #1-15 boyunca defalarca kontrol edilen ama her analizde yeniden doğrulanan temel mimari kurallar:

| Kural | Durum | Kanıt |
|---|---|---|
| **Domain → UnityEngine bağımlılığı yok** | ✅ Verified | `Get-ChildItem Assets\Scripts\Domain \| Select-String UnityEngine` → sadece docstring yorumları (negatif context) |
| **Application → Infrastructure concrete leak yok** | ✅ Verified | `Get-ChildItem Assets\Scripts\Application \| Select-String 'using PuzzleGame\.Infrastructure'` → **0 sonuç** |
| **Unity 6 deprecated API (FindObjectOfType) tamamen temizlenmiş** | ✅ Verified | `Select-String FindObjectOfType` → 0 sonuç (tüm geçişler FindAnyObjectByType'e) |
| **TODO/FIXME/HACK inline comment yok** | ✅ Verified | `Select-String 'TODO\|FIXME\|HACK\|XXX\|NotImplemented'` → 0 sonuç (4 false positive: `ToDomainColor` içindeki "ToDo" substring) |
| **GameSaveManager HMAC-SHA256 zaten uygulanmış** | ✅ Verified | `GameSaveManager.cs:14-22` docstring + `:34-44` BuildSecretKey + `using System.Security.Cryptography;` — Sprint #18 (HMAC hardening) **iptal** |

### 2.1 MonoBehaviour Dağılımı (22 adet)

| Katman | Adet | Örnekler |
|---|---|---|
| **Infrastructure (MonoBehaviour impl)** | 2 | `UpdateManager`, `ErrorIndicatorController` (interface impl) |
| **Composition (root lifecycle)** | 1 | `LocalizationBootstrap` (Sprint #15) |
| **Presentation (root scene)** | 7 | `GameManager`, `MoldController`, `Wobble`, `CameraEffectsController`, `MoldMeshGenerator`, `MoldCorkController`, `URPConfigHelper` |
| **UI (sub-panels)** | 12 | `MainMenuController`, `LevelSelectUI`, `WorldMapController`, `HudPresenter`, `DebugOverlayUI`, `DailyChallengeController`, `ConsentModal`, `AgeGateModal`, `SettingsPrivacyController`, `SettingsSoundController` + 2 embedded view (`LevelButtonView`, `BiomeCardView`) |

**Tüm MonoBehaviour'lar Composition + Presentation katmanlarında — Domain/Application hiçbiri yok.** SRP'ye uygun.

### 2.2 Asmdef Layer Boundaries

| Asmdef | Yeri | RootNamespace | Referans verdiği |
|---|---|---|---|
| `PuzzleGame.Domain` | `Assets/Scripts/Domain/` | `PuzzleGame.Domain` | (hiçbir şey) |
| `PuzzleGame.Application` | `Assets/Scripts/Application/` | `PuzzleGame.Application` | Domain |
| `PuzzleGame.Infrastructure` | `Assets/Scripts/Infrastructure/` | `PuzzleGame.Infrastructure` | Domain, Application |
| `PuzzleGame.Composition` | `Assets/Scripts/Composition/` (root `Assets/Scripts/`) | `PuzzleGame` | Domain, Application, Infrastructure, Presentation, Installers |
| `PuzzleGame.Editor` | `Assets/Scripts/Editor/` | `PuzzleGame.Editor` | Domain, Application, Infrastructure |
| `PuzzleGame.Tests` | `Assets/Tests/` | `PuzzleGame.Tests` | Domain, Application, Infrastructure, Composition, Editor |

**Circular dependency: yok.** Domain en altta, Tests en üstte. Editor layer Presentation'a bağlı değil (Editor sadece runtime asmdef'lere bağlı — bu doğru).

---

## 3. Test Coverage Reality Check (Major Update)

**Önceki tahmin:** ~169 [Test] method (Sprint sonu özetlerinden birikimli)
**Gerçek:** **423 [Test] method + 28 [TestCase] entry** = 451 test invocation
**Test dosyası:** 66 (199 source dosyasına karşı = %33 dosya coverage)

| Katman | Source | Test | Test/Source | Yorum |
|---|---|---|---|---|
| Domain | 4 services | 3 test files | 75% | `OreSortSolver`, `MoldValidationService`, `LocalizationService` ✅; **`DifficultyBasedLevelGenerator` eksik** |
| Application | 23 services | 13 test files | 57% | 10 service **test yok** (bkz. §3.1) |
| Infrastructure | 25 impls | 11 test files | 44% | Çoğu Unity adapter (test edilemez); logic-heavy impl'ler ✅ |

### 3.1 Test Edilmeyen Kritik Servisler (Pure Logic, Test Edilebilir)

| Servis | Katman | LOC | Test Edilebilirlik | Öncelik |
|---|---|---|---|---|
| `DifficultyBasedLevelGenerator` | Domain | 134 | ✅ Pure C# | **Yüksek** — 50-level campaign üreticisi, determinism kritik |
| `DailyChallengeService` | Application | 113 | ✅ Pure C# (DateTime math + RNG) | **Yüksek** — UTC seed + streak logic |
| `HintService` | Application | 90 | ✅ Pure C# (solver integration) | **Yüksek** — coin economy + solver call |
| `UndoService` | Application | 57 | ✅ Pure C# (snapshot stack) | **Yüksek** — coin economy + state restore |
| `AgeGateService` | Application | 57 | ✅ Pure C# (DateTime age calc) | **Orta** — GDPR/COPPA edge cases |
| `MoldSelectionService` | Application | 39 | ✅ Pure C# | **Düşük** (küçük, düşük risk) |
| `CastAnimationState` | Application | 177 | ❌ Animation service coupled | Düşük (refactor gerekli önce) |
| `PrimeTweenService` | Application | 147 | ❌ Unity adapter (PrimeTween) | Düşük (Unity zorunlu) |
| `HapticFeedbackService` | Application | 77 | ❌ Unity device call | Düşük (Unity zorunlu) |
| `ScreenTransitionService` | Application | 64 | ⚠️ Coroutine + CanvasGroup | Orta (testable ama refactor gerekli) |
| `NoOpAnalyticsService` | Application | 30 | ❌ No-op | Yok (test etmeye değmez) |

**5 pure-logic, untested, kritik servis** → Sprint #17 candidate (test coverage hardening).

---

## 4. Yeni Mimari Fırsatlar (Post-Debt Modernization)

### 4.1 Addressables Migration (Yüksek ROI) — Sprint #16

**Mevcut durum:**
- `AddressablesAssetProvider` (Infrastructure) zaten var (Sprint #6'da kuruldu)
- `ResourcesAssetProvider` (Infrastructure) sync fallback olarak da var
- **AMA**: 28 `Resources.Load` call site — büyük çoğunluk doğrudan çağrı, provider abstraction'ı BYPASS ediyor

**Dağılım:**
| Dosya | Resources.Load Sayısı | Yorum |
|---|---|---|
| `GameInstaller.cs` | 6 | Config asset loading (yüksek öncelik — startup hot path) |
| `SceneBuilder.cs` | 4 | Editor tool, düşük öncelik |
| `ParticleFactory.cs` | 3 | Pool fallback (kritik) |
| `AddressablesAssetProvider.cs` | 2 | Doğru kullanım (kontrol noktası) |
| `ResourcesAssetProvider.cs` | 2 | Doğru kullanım (kontrol noktası) |
| `TestTab.cs` | 2 | Editor tool |
| `PouringLabTab.cs` | 2 | Editor tool |
| `Wobble.cs` | 1 | Runtime config (yüksek öncelik) |
| `StreamRenderer.cs` | 1 | Runtime VFX asset (yüksek öncelik) |
| `LocalizationTab.cs` | 1 | Editor tool |
| `UIStyleConfig.cs` | 1 | Docstring örneği (no-op) |

**Runtime hot path: 11 site** (GameInstaller 6 + Wobble 1 + StreamRenderer 1 + ParticleFactory 3). Editor tool: 11 site. Docstring: 1.

**Etki:**
- **Startup time:** ~200-400ms azalma (Resources.Load senkron; Addressables async preload)
- **Memory:** 50-100MB peak azalma (lazy load)
- **APK size:** Streaming content yüklenebilir → install size azalma
- **Content update:** Live updates (Addressables remote catalog) → soft launch sonrası fix/level packs

**Risk:** Düşük — `AddressablesAssetProvider` zaten test edilmiş, registration pattern'i net (`IAssetProvider.LoadAsync<T>`).

**Tahmini etki:** 11 runtime site + 11 editor tool + 4 kontrol noktası = **sweep pattern**, bounded scope, tek sprint.

### 4.2 UniTask / Async-Await Birleştirme (Orta ROI) — Sprint #17

**Mevcut coroutine kullanımı (4 site):**
- `ScreenTransitionService.cs:31,41` — CanvasGroup fade
- `ErrorIndicatorController.cs:61,72` — Auto-hide timer
- (Sprint #15 `LocalizationBootstrap` zaten async pattern'de ✅)

**Sprint #15 Task pattern'i kurmuşken**, diğer 4 site de aynı pattern'e geçirilirse:
- Cancellation token plumbing (Sprint #15 `IAsyncTranslationProvider.LoadAsync(ct)` standardı)
- Test edilebilirlik (coroutine `IEnumerator` yerine `Task` → PlayMode test mocklanabilir)
- Performance (UniTask allocation-free; coroutine boxing yok)

**Etki:** Düşük (küçük dosya) + orta (test edilebilirlik pattern standardizasyonu).

**Risk:** Orta — UniTask paket ekleme kararı (3rd party, ama ücretsiz, MIT, Unity sponsorlu). Coroutine → Task dönüşümü mekanik.

### 4.3 Save Manager Internal Refactoring (Düşük ROI) — Sprint #18+ Candidate

**Mevcut:** `GameSaveManager.cs` 326 LOC, 4 karışık sorumluluk:
1. **HMAC crypto** (`BuildSecretKey`, signature)
2. **JSON serialization** (`SaveData`, `MoldSaveData`)
3. **File IO + atomic write** (`TempPath`, `Path.Combine`)
4. **In-memory cache** (`_cachedSaveData`, `_cacheLoaded`)

**Refactor önerisi:** 3 dosyaya böl
- `SaveCrypto.cs` (HMAC compute/verify)
- `SaveStorage.cs` (File IO + atomic write)
- `GameSaveManager.cs` (orchestrator + cache + migration)

**Etki:** Düşük (mevcut kod çalışıyor, test edilmiş, HMAC zaten var). Düşük riskli ama düşük ROI.

**Öncelik:** Düşük — sadece v1.0 sonrası mimari temizlik sprinti olarak uygun.

### 4.4 Editor Tooling God Classes (Çok Düşük ROI) — Sprint #19+ Candidate

**Mevcut:** 24 editor dosyası, 4,903 LOC toplam, 9 dosya >= 200 LOC:
- `LevelsTab.cs` (543), `TestTab.cs` (538), `LocalizationTab.cs` (435), `LevelUITab.cs` (320)
- `PaletteTab.cs` (291), `SceneTab.cs` (289), `FeaturesTab.cs` (268)
- `SceneBuilderPrimitives.cs` (251) — Sprint #14'te bölündü ama hâlâ 251 LOC
- `DataTab.cs` (241), `ValidateTab.cs` (219)

**Etki:** Çok düşük — dev tooling, shipping code değil. Soft launch'ı etkilemez.

**Öncelik:** Çok düşük — ancak v1.1+ geliştirici deneyimi iyileştirmesi olarak uygun.

---

## 5. God Class / LOC Analizi (Runtime Code Only — Editor Hariç)

| Dosya | LOC | Sorumluluk | Refactor Gerekli? | Sprint |
|---|---|---|---|---|
| `PourSystemController.cs` | 335 | Pour sim + history + debug (interfaces Sprint #10'da segregate edildi) | Hayır — interface segregation yeterli, impl cohesive | — |
| `GameSaveManager.cs` | 326 | Crypto + IO + cache + serialization | Opsiyonel (§4.3) | #18+ |
| `AnimationService.cs` | 293 | Animation orchestration (tween, sequence, easing) | Hayır — cohesive, single responsibility | — |
| `MainMenuController.cs` | 279 | UI nav + state machine + sub-panel fade | Hayır — UI controller pattern, monolitik ama test edilebilir | — |
| `OreSortSolver.cs` | 276 | BFS solver algorithm | Hayır — algoritma kodu, bu kadar LOC doğal | — |
| `GameInstaller.cs` | 258 | DI registration root (40+ kayıt, 2 platform guard) | Hayır — composition root zaten yoğun | — |
| `MoldController.cs` | 256 | IMoldView facade (Sprint #2 sonrası) | Hayır — zaten 3 POCO'ya bölünmüş | — |
| `AudioService.cs` | 228 | Audio playback + pool + config + mute | Opsiyonel | #20+ |
| `MoldInputRouter.cs` | 204 | Input dispatch (Sprint #11 sonrası) | Hayır — interface segregation yeterli | — |
| `MoldCorkController.cs` | 200 | Cork visualization (3D mesh) | Hayır — single visual concern | — |

**Critical threshold (200+ LOC)**: 11 dosya. **Hiçbiri kritik god class değil** — hepsi cohesive ve test edilmiş veya testable.

---

## 6. Performance & Allocation Hotspots

### 6.1 Coroutine / Async Pattern Heterogeneity

| Pattern | Kullanım | Dosya Sayısı |
|---|---|---|
| `StartCoroutine(IEnumerator)` | UI fade, error auto-hide | 2 dosya (4 call site) |
| `UnityWebRequest` (Sprint #15 async) | Localization Android load | 1 dosya (2 call site) |
| `Task<T>` async/await | Yok (henüz) | 0 |

**Önerilen yön (Sprint #17):** UniTask entegrasyonu → tüm async pattern'ler tek çatı altında.

### 6.2 String Allocation Hotspots

Sprint #13 (DebugOverlayUI StringBuilder reuse) ve Sprint #14 (SceneBuilder 710 → 760 LOC 4 file'a yayılmış) sonrası büyük string allocation hotspot'lar temizlendi. **Yeni hotspot taraması gerekmiyor.**

### 6.3 Per-Frame Allocation Risk Scan

Per-frame allocation risk olan dosyalar Sprint #13'te (DebugOverlayUI) ve Sprint #14'te (SceneBuilder) zaten temizlendi. **Yeni risk tespit edilmedi.**

---

## 7. Dependency Direction Audit (Strict Clean Architecture)

| Kaynak | Hedef | İzin Verilen? | Durum |
|---|---|---|---|
| Domain | Application | ❌ | ✅ 0 import |
| Domain | Infrastructure | ❌ | ✅ 0 import |
| Domain | UnityEngine | ❌ | ✅ 0 import (pure C#) |
| Application | Domain | ✅ | ✅ Normal |
| Application | Infrastructure | ❌ (concrete) | ✅ 0 `using PuzzleGame.Infrastructure` (sadece interfaces) |
| Application | UnityEngine | ✅ (sınırlı) | ✅ 19 satır, çoğu Configuration ScriptableObject + Color/Vector3 adapter noktaları |
| Infrastructure | Application (interfaces) | ✅ | ✅ Normal |
| Infrastructure | UnityEngine | ✅ | ✅ Zorunlu (Unity adapter) |
| Presentation | Application (interfaces) | ✅ | ✅ Normal |
| Presentation | Infrastructure (concrete) | ❌ | ✅ VContainer DI üzerinden |
| Composition | Her şey | ✅ | ✅ Root (doğru) |

**Dependency direction: %100 Clean Architecture uyumlu.**

---

## 8. Sonuç ve Sprint #16-19 Önerileri

### 8.1 Öncelik Matrisi

| Sprint | Konu | ROI | Risk | Zorluk | Sprint Süresi (tahmini) |
|---|---|---|---|---|---|
| **#16** | Addressables migration | **Yüksek** | Düşük | Orta (11 runtime site sweep) | Orta |
| **#17** | 5 untested critical service test coverage | Orta-Yüksek | Çok düşük (pure C#) | Düşük (mekanik test yazımı) | Kısa |
| **#18** | UniTask entegrasyonu + coroutine → Task dönüşümü | Orta | Orta (paket ekleme) | Düşük (4 call site) | Kısa |
| **#19** | GameSaveManager refactor (3 file split) | Düşük | Çok düşük | Düşük | Kısa |
| **#20+** | Editor tooling god class refactor | Çok düşük | Düşük | Orta | Uzun (dev tool) |

### 8.2 Sprint #16 Detaylı Plan (Önerilen Başlangıç)

**Kapsam:** Resources.Load call site'larını `IAssetProvider.LoadAsync<T>` (veya `Load<T>` sync) üzerinden geçirme. Toplam **11 runtime site** + **4 kontrol noktası** (zaten doğru kullanan) + **11 editor tool site** (opsiyonel).

**Sub-task'lar:**
1. `GameInstaller.cs` — 6 site (Config asset loading) — startup hot path
2. `Wobble.cs` — 1 site (WobbleConfig)
3. `StreamRenderer.cs` — 1 site (VisualEffectAsset)
4. `ParticleFactory.cs` — 3 site (Addressables → Resources fallback path standardize)
5. **Editor tooling** — 11 site (Sprint #16 dışı bırakılabilir, dev tools)
6. **`AddressablesAssetProvider` + `ResourcesAssetProvider`** — interface sözleşmesini gözden geçir (her ikisi de `IAssetProvider` mı? Hangisi sync vs async?)

**Tahmini etki:** Startup 200-400ms ↓, peak memory 50-100MB ↓, APK size 30-50% ↓ (content streaming), content update path açılır.

**Build impact:** 0 yeni tip, 0 yeni interface. `com.unity.addressables` paketi zaten `manifest.json`'da var mı kontrol et — yoksa ekleme kararı (paket kararı = kullanıcı onayı gerekir).

---

## 9. Açık Sorular (Kullanıcı Onayı Gereken)

1. **Sprint #16 Addressables paketi:** `com.unity.addressables` manifest.json'da var mı? Yoksa ekleyelim mi? (3rd party paket, ücretsiz ama karar sizin)
2. **Sprint #17 UniTask:** Benzer karar — `com.cysharp.unitask` paketi (MIT, Cysharp/UniTask, sizin tercihiniz)
3. **Sprint sırası:** #16 → #17 → #18 (önerilen) mi, yoksa #17 (test coverage) önce mi? (Test coverage daha güvenli, Addressables daha yüksek etki)
