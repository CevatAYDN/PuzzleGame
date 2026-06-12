# Code Standards — PuzzleGame

These are the rules every change must respect. They are derived from the project's README and the existing layout — read them as the contract, not a wish list.

## Clean Architecture boundaries

The solution has six projects in `PuzzleGame.slnx`:

| Project | Path | What lives here | Allowed dependencies |
|---|---|---|---|
| `PuzzleGame.Domain` | `Assets/Scripts/Domain/` | Pure puzzle logic — `BottleState`, `LiquidLayer`, level generation, validation, solver, undo, progression, localization | **None.** Pure C# only. |
| `PuzzleGame.Application` | `Assets/Scripts/Application/` | Use cases, orchestration, services, UI components, events, logging, animation/audio/input handlers, ScriptableObject configuration | `Domain` only. |
| `PuzzleGame.Infrastructure` | `Assets/Scripts/Infrastructure/` | Concrete implementations of Application/Domain interfaces (color adapter, providers, object pools) | `Domain` + `Application`. Implements their interfaces. |
| `PuzzleGame.Composition` | `Assets/Scripts/Installers/` | VContainer DI graph (`GameInstaller`) | `Domain` + `Application` + `Infrastructure`. The only project that wires concretes. |
| `PuzzleGame.Editor` | `Assets/Scripts/Editor/` | Unity Editor tools, build menu, custom inspectors | `Domain` + `Application` + `Infrastructure` (read-only usually) + `UnityEditor` APIs. |
| `PuzzleGame.Tests` | `Assets/Tests/` | NUnit tests + hand-written Fakes | All of the above + NUnit + Unity Test Framework. |

**Hard rules:**

- `Domain` **must not** reference `UnityEngine.*`, `VContainer`, `PrimeTween`, or any Unity package. The point of the Domain is to compile and test without Unity.
- `Application` **must not** reference `Infrastructure` concretes — only interfaces.
- `Infrastructure` does not reference `Application`/`Infrastructure` "upward" — it only **implements** interfaces from the layers above.
- `Composition` is the only project that registers concrete implementations with their interfaces (VContainer).

## SOLID, in this codebase

- **Single Responsibility:** one service, one reason to change. Reject `XxxManager` / `XxxHelper` / `XxxUtils` classes that grow unbounded.
- **Open/Closed:** new behaviour = new interface + new implementation, not edits to a switch statement. The `ITweenService` swap (PrimeTween vs. coroutine fake) is the model.
- **Liskov:** any `IFoo` consumer must work with every `Foo` implementation. Don't put Unity-specific assumptions into shared interfaces.
- **Interface Segregation:** small interfaces. `IBottleSelectionService`, `IPourService`, `IAnimationService` — not `IGameUseEverything`.
- **Dependency Inversion:** constructor injection of interfaces. `new SomeConcrete()` inside `Application/` is a code-review block.

## Naming

| Kind | Convention | Example |
|---|---|---|
| Interface | `IXxx` | `IBottleValidator`, `ILevelGenerator` |
| Service | `XxxService` | `BottleValidationService`, `LevelSetupService` |
| State / entity | `XxxState` / `XxxModel` | `BottleState`, `GameState`, `LiquidLayer` |
| MonoBehaviour view | `XxxView` | `BottleView` (NOT `BottleBehaviour`) |
| VContainer installer | `XxxInstaller` | `GameInstaller` |
| ScriptableObject config | `XxxConfig` | `LevelCatalogConfig` |
| Test fixture | `XxxTests` | `BottleValidationServiceTests` |
| Test double | `FakeXxx` | `FakeBottleValidator`, `FakeAnimationService` |
| Editor tool | `XxxWindow` / `XxxEditor` | `PuzzleGameEditorWindow` |

## File layout

- One public type per file, filename = type name.
- Group inside a layer: `Interfaces/`, `Models/`, `Services/`, `Configuration/`, `Events/`, `Logging/`, `UI/`, `Pool/`, `Providers/`, `Implementations/`.
- `BottleConstants.cs` and similar constants files live at the layer root, not in a `Constants/` subfolder (project convention).
- `*.asmdef` files live next to the code they describe — do not move them.

## Style

- 4-space indent, LF line endings (Unity default; do not force CRLF).
- `using` directives at the top, sorted.
- `readonly` fields, immutable models, factory methods (`BottleState.Create(...)`, `LiquidLayer.Create(...)`).
- No `var` for primitive types; use `var` for non-trivial generics or anonymous types.
- No regions unless the file already uses them.
- No `// TODO` without a ticket / owner.

## Unity-specific

- Never `GameObject.Find` / `FindObjectOfType` in runtime hot paths. Use DI or direct references.
- Never `Instantiate` inside `Update` / `LateUpdate` / a tween callback. Pool via `Infrastructure/Pool/GameObjectPool.cs`.
- Animation: PrimeTween only. No `DOTween`, no `Coroutine` for visual polish.
- Input: `com.unity.inputsystem` only. Never the legacy `Input` class.
- Localization: `ILocalizationService.GetString("key")`. Never hard-code UI strings in views.
- Commit `.meta` files alongside their assets; never hand-edit a GUID.

## Accessibility (Erişilebilirlik)

- **Renk-bağımsız tasarım:** Renk ile ayırt edilen her öğe (sıvı, buton, ikon) için şekil/desen/ikon alternatifi zorunludur. `DomainColor` yanında `DomainPattern` / `DomainIcon` tanımlanmalı.
- **Kontrast oranı:** UI metinleri ve interaktif öğeler WCAG 2.1 AA minimum 4.5:1 kontrast oranını sağlamalı.
- **Dokunma hedefi:** Tüm dokunulabilir öğeler minimum 44×44pt.
- **Reduced motion:** Animasyonlar `IAccessibilityService.IsReducedMotionEnabled` kontrolü ile basitleştirilebilmeli.
- **Screen reader:** UI öğelerinin `accessibilityLabel` / `contentDescription` tanımları zorunludur.
- **Fail Fast:** Erişilebilirlik alternatifi tanımlanmamışsa `MissingAccessibilityFallbackException` fırlatılır.

## Localization (Lokalizasyon)

- **Hardcoded string toleransı sıfır.** UI'da görünen hiçbir metin doğrudan string olarak yazılmaz. Tümü `ILocalizationService.GetString("key")` üzerinden çekilir.
- **Eksik key → Fail Fast:** Lokalizasyon key'i bulunamazsa `MissingLocalizationKeyException` fırlatılır (sessiz fallback yasak).
- **RTL düzen desteği:** Arapça/İbranice gibi sağdan-sola dillerde UI düzeni doğru çalışmalı.
- **Font fallback zinciri:** Latin → CJK → Arabic → Devanagari sırasında font'lar yüklenebilmeli.
- **Dinamik UI genişleme:** Almanca gibi uzun çeviriler için UI öğeleri taşmamalı (overflow test).

## Security (Güvenlik)

- **Save dosyası koruması:** Oyun kayıtları HMAC-SHA256 checksum ile korunur. Manipüle edildiğinde `SaveCorruptionException` fırlatılır.
- **IAP receipt doğrulama:** Client-side receipt → Backend API → Google Play Developer API zinciri. Client-side tek başına güvenilmez.
- **PlayerPrefs'te hassas veri yasak.** Token, şifre veya kişisel veri asla `PlayerPrefs`'te veya açık dosyada saklanmaz.
- **Input sanitization:** Kullanıcı girdisi (isim, skor vb.) sanitize edilir.
- **Leaderboard anti-cheat:** Skorlar server-authoritative validation ile doğrulanır.

## Competitive standards (Rekabet)

- **APK boyutu hedefi:** İlk indirme ≤50MB. Unity Addressables, ASTC texture sıkıştırma, code stripping aktif.
- **Haptic feedback:** Her dokunma etkileşimi `IHapticService` üzerinden haptic pattern tanımı içermeli.
- **Analitik event:** Her yeni kullanıcı etkileşimi `IAnalyticsService.LogEvent()` ile işaretlenmeli. Zorunlu event'ler: `level_started`, `level_completed`, `level_failed`, `tutorial_step_N`, `ad_shown`, `iap_purchased`, `accessibility_mode_changed`, `rating_prompt_shown`.
- **Meta-game hook:** Yeni özellikler meta-game ilerleme sistemiyle (`IProgressionService`) entegre edilmeli.
- **Solvability:** Prosedürel üretilen her bölüm `ISolvabilityChecker.IsSolvable()` ile doğrulanmalı.

