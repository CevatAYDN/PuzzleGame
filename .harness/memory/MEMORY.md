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
