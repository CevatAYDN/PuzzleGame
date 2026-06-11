# Project Memory — PuzzleGame

Shared lessons that the whole team (orchestrator + every rein) should know. Append-only — to modify or remove an entry, edit this file directly.

Format:

```
### <topic> (<YYYY-MM-DD>)
Type: <architecture | gotcha | convention | dependency | tool>
Reins affected: <names>
<lesson>
WHY: <why this matters later>
```

## Architecture

### Domain layer is Unity-free (2026-06-03)
Type: architecture
Reins affected: game-logic-expert, code-reviewer, tester
`Assets/Scripts/Domain/**` must not import `UnityEngine.*`, `VContainer`, `PrimeTween`, or any Unity package. The whole point of the Domain is that it compiles and tests without the Unity runtime. If a Domain class needs a `Color`, the seam is `DomainColor` (enum) + the `ColorAdapter` in Infrastructure.
WHY: A Domain test that imports `UnityEngine` cannot run via `dotnet test` and signals the boundary is broken.

### Composition root is `Assets/Scripts/Installers/GameInstaller.cs` (2026-06-03)
Type: architecture
Reins affected: unity-expert, code-reviewer
VContainer registrations go in `GameInstaller.cs` and nowhere else. New interface in `Application/Interfaces/` → implementation in `Application/Services/` or `Infrastructure/Implementations/` → register in `GameInstaller`.
WHY: Anything else makes the DI graph untestable and untraceable.

## Gotchas

### `.meta` files are part of the asset (2026-06-03)
Type: gotcha
Reins affected: unity-expert, developer
Every new `Assets/Scripts/**` file gets a `.meta` companion (Unity auto-generates one on first refresh). Commit them. Never hand-edit a GUID. If a `.meta` is missing, the asset will be re-imported with a fresh GUID and lose references.
WHY: Lost `.meta` GUIDs silently break scene / prefab / asset references.

### PrimeTween, not DOTween or coroutines (2026-06-03)
Type: convention
Reins affected: unity-expert
Visual polish uses `PrimeTween.Tween.*`. There is no DOTween in the project, and coroutines for visual polish fight the zero-allocation design. Use `Tween.Custom` for custom curves.
WHY: `Update` / tween callbacks must stay allocation-free for mobile.

## Conventions

### Hand-written `Fake*` doubles, no mocking library (2026-06-03)
Type: convention
Reins affected: tester, code-reviewer
Test doubles live in `Assets/Tests/Fakes/` and follow the `FakeXxx` naming. Do not add Moq / NSubstitute / FakeItEasy.
WHY: Determinism, zero third-party surface, faster compile.

### Conventional Commits with scope (2026-06-03)
Type: convention
Reins affected: developer, unity-expert, game-logic-expert
Commit messages use `type(scope): summary` (e.g. `feat(domain): add difficulty-based seed range`). See `.harness/docs/git-workflow.md`.
WHY: Lets `git log --grep` and changelog generation work without extra tooling.

### Council Mode is opt-in, not default (2026-06-08)
Type: convention
Reins affected: harness (orchestrator)
The orchestrator supports a 5-persona "Board of Directors" review mode (Lead Architect / Mechanics / Performance / UI-UX / QA). It is **only** triggered by explicit user keywords (`kurul`, `council`, `kurul oturumu`, `meclis`, `5 kişilik kurul`, `board of directors`). Format and rules live in `.harness/agent.md` → "Council Mode" section. Personas are role-plays, not separate LLM calls — project context stays unified.
WHY: A 5-persona debate is expensive in tokens and noisy for trivial tasks. Keeping it opt-in preserves the default "route to one rein" flow and respects the user's "be concise" preference.

### IMoldValidator.CanBreakCork added — update all implementors (2026-06-11)
Type: architecture
Reins affected: game-logic-expert, unity-expert, tester, code-reviewer
`IMoldValidator` gained a `bool CanBreakCork(MoldState source, MoldState target)` method. Every class implementing `IMoldValidator` must implement it: `MoldValidationService` (Domain), `FakeMoldValidator` (Tests/Fakes), and the private `FakeMoldValidator` in `EditorServiceLocatorTests.cs`. Missing implementations break the build with CS0535.
WHY: Interface changes are breaking changes. All implementors must be updated atomically — missing one produces compiler errors across 3 csproj files.

### .NET 10 breaks Unity RP Core NativeArray→ReadOnlySpan implicit cast (2026-06-11)
Type: dependency
Reins affected: unity-expert, developer
.NET 10's C# compiler (Roslyn) enforces stricter ref-safety analysis. `NativeArray<PassData>` implicit conversion to `ReadOnlySpan<PassData>` triggers CS8347/CS8168. Fix: replaced `NativeArray<PassData>` with managed `PassData[]` in `Library/PackageCache/com.unity.render-pipelines.core@.../Runtime/RenderGraph/Compiler/PassesData.cs:792-805`. This fix lives in `Library/PackageCache/` and will be overwritten when Unity regenerates package cache. Long-term: upgrade Unity 6 to a version that bundles a .NET 10-compatible RP Core package.
WHY: Clearing the Library folder or running Unity package refresh will restore the original buggy code and break `dotnet build`. Need a permanent solution.

### Unity csproj has EnableDefaultItems=false — new .cs files need manual Compile entries (2026-06-11)
Type: gotcha
Reins affected: developer, unity-expert
Unity-generated csproj files (`PuzzleGame.*.csproj`) use `<EnableDefaultItems>false</EnableDefaultItems>` and list every `.cs` file manually with `<Compile Include="...">`. New files created outside Unity Editor (e.g., via IDE or CLI) are NOT automatically included and cause CS0246 build errors. Fix: manually add `<Compile Include="Assets/Scripts/...">` to the relevant csproj, or regenerate from Unity Editor.
WHY: Every new `.cs` file that's not auto-detected by Unity's AssetDatabase will silently fail to compile until the csproj is updated.

### Sprint C complete: Generator, Achievement rewards, PowerUpUI (2026-06-11)
Type: architecture
Reins affected: game-logic-expert, unity-expert, developer
Sprint C (Game Mechanics) items are complete and building with 0 errors:
- **C.1 — Generator frozen layers + multi-pour**: `DifficultyBasedLevelGenerator.GenerateSolvable()` now supports `enableFrozenLayers` flag (buried frozen ore for mid+ difficulty) and `enableMultiPour` flag (requires multiple separate pours to mix colors). Both guarded by `mixFactor >= 0.5f` (medium+ difficulty). Build verified.
- **C.2 — Achievement coin rewards**: `AchievementService` injects `ICoinWallet`. `RewardCoins[]` static array maps each `AchievementId` to a coin amount (10–200). On unlock, calls `_wallet.Add(reward, $"achievement_{id}")`. Both `IncrementProgress` and `SetProgress` paths grant the reward.
- **C.3 — PowerUpUI**: New `PowerUpUI` + `PowerUpSlotView` MonoBehaviours in `Presentation/UI/`. Shows all 5 power-up types with charge counts, activation buttons blocked during animation (matching `HudPresenter` pattern). Registered in `PresentationInstallerModule` via `FindOrFallback<PowerUpUI>`. Required manual csproj edit for new file inclusion (see gotcha above).
WHY: Tracks that Sprint C game-mechanics work is shipped and building.
