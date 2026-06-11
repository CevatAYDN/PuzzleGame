# TODO - Unity Editor "Tam Kontrol" Geliştirmeleri & Production Yol Haritası

## Step 1 — Playback / PlayMode çakışmasını düzelt
- [x] Playback sırasında Play'e geçişte playback'i otomatik durdur
- [x] Play sırasında (EditorApplication.isPlaying) playback UI/engine aktif olmasın
- [x] Playback UpdatePlaybackLoop içinde hızlı return + state reset güvenliği
- [x] "Load" / "Play" / "InitPlayback" akışlarında tutarlı state yönetimi

## Step 2 — Solve/Verify/Reseed/Optimize işlerini geliştir
- [x] Tek seferde sadece bir uzun işlem çalışsın (reentrancy guard)
- [x] İptal butonu / iptal bayrağı ekle
- [x] İş boyunca ForgeEditorWindow status bar'ı güncelle
- [x] ProgressBar + ClearProgressBar her koşulda garantilensin

## Step 3 — Editör İyileştirmeleri
- [x] Validate aksiyonlarını güçlendir (quick fix/ping)
  - `ValidateTab.cs`: QuickFixType enum + ApplyQuickFix metodu eklendi
  - PingAsset, SelectObjects, EnableGpuInstancing, CreateMissingData aksiyonları
  - Her başarısız validation result için "⚡ Quick Fix" ve "🔍 Ping/Select" butonları
- [x] Level load/export "aktif scene yönetimi + Undo grubu standardı" iyileştirme
  - `SceneBuilder.cs`: zaten `Undo.GetCurrentGroup()` + `Undo.SetCurrentGroupName()` kullanıyor
  - `LevelsTab.cs`: `LoadLevelIntoScene` zaten Undo grubu ile sarılı
  - `SceneTab.cs`: paint/clear/pop işlemleri `Undo.RecordObject` kullanıyor
- [x] Global hotkey / refresh / tab switching iyileştirmeleri
  - `ForgeEditorWindow.cs`: `HandleKeyboardShortcuts()` eklendi
  - Ctrl+Tab / Ctrl+Shift+Tab: tab geçişi
  - F5 / Ctrl+R: refresh
  - `Event.current.Use()` ile event tüketimi

## Step 4 — Production Readiness & Release Checklist (Kurul Kararı)
- [x] **Asenkron Girdi Kilidi (Input Lockout):**
  - `MoldInputRouter.ProcessInput()`: `_animationService.IsAnimating` kontrolü mevcut
  - `UndoService.TryUndo()`: `_animationService.IsAnimating` kontrolü mevcut
  - `HintService.TryGetHint()`: `_animationService.IsAnimating` kontrolü mevcut
  - `PowerUpService.Activate()`: `_animationService.IsAnimating` kontrolü eklendi (yeni)
  - `PowerUpService`: DI kaydı `GameplayInstallerModule`'a eklendi (eksikti)
- [x] **GPU Instancing Doğrulaması:**
  - `RendererService.cs`: `MaterialPropertyBlock` ile render — GPU Instancing bozulmaz
  - `ValidateTab.cs`: GPU Instancing validation + "⚡ Quick Fix" (EnableGpuInstancing) eklendi
- [x] **Hata Geri Alım Testleri (Reaction Undo QA):**
  - `UndoServiceTests.cs`: 5 yeni test (animasyon toggle, limit exhaust, level reset, chain undo)
  - `ReactionServiceTests.cs`: 4 yeni test (chain reaction, post-explosion state, empty mold, single layer)

## Step 5 — Global Gameplay Mechanics (Magic Sort Alignment)
- [x] **Ek Kalıp (Extra Mold Power-up):**
  - `MoldPoolInitializer`: `IEventAggregator` enjekte edildi, `PowerUpActivatedEvent` dinleyici
  - `ActivateExtraMold()`: havuzdan pasif mold'u aktive eder, tüm sistemlere wire'lar (history, input, error, wobble)
  - `GameHistoryManager`: zaten dynamic mold count destekli (snapshot resize)
- [x] **Gizli Katmanlar (Hidden Layers):**
  - `OreLayer`: `IsHidden` field + `WithHidden()` factory
  - `RendererService.UpdateOre()`: `IsHidden` layer'lar gri renkte render edilir
  - `OreSortSolver`: IsHidden flag'i görmezden gelir (sadece görsel mekanik)
- [x] **Dinamik Tıpa Kilidi (Cork Lock):**
  - `MoldState`: `CorkColor` (OreColor) + `HasCork` + `SetCork()` + `BreakCork()`
  - `IMoldValidator`: `CanBreakCork(source, target)` eklendi
  - `MoldValidationService.CanCast()`: cork'lu mold'e sadece boşsa ve cork rengi eşleşirse cast
  - `OreSortSolver.OreSortSolverOptions`: `CorkColors` dizisi
  - `OreSortSolver.Solve()`: BFS loop'ta cork kontrolü — cork rengi eşleşmezse cast yapılamaz
  - `CastService.TryCast()`: cork break + `CanBreakCork` kontrolü

---

## Ek Bulgular (Code Review sırasında tespit edilen ve düzeltilen)

### EventAggregator WeakReference Bug (Critical)
- **Sorun**: `Subscription<T>` constructor'ında `action.Target` null olduğunda (static method, lambda capturing no instance) `WeakReference(null).IsAlive` false döner → subscription ölü sayılır, event'ler sessizce kaybolur.
- **Düzeltme**: `_isStaticOrLambda` flag eklendi. `action.Target == null` ise `IsAlive` her zaman true döner.
- **Dosya**: `Assets/Scripts/Application/Events/EventAggregator.cs`

### PowerUpService DI Kaydı Eksik (Medium)
- **Sorun**: `PowerUpService` hiçbir installer module'da register edilmemişti. `IPowerUpService` enjekte edilmeye çalışıldığında VContainer exception fırlatırdı.
- **Düzeltme**: `GameplayInstallerModule.Configure()` içine `builder.Register<IPowerUpService, PowerUpService>(Lifetime.Singleton)` eklendi.
- **Dosya**: `Assets/Scripts/Installers/GameplayInstallerModule.cs`
