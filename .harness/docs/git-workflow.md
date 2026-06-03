# Git Workflow — PuzzleGame

## Branches

- `main` is the only long-lived branch. It is always green (compiles, tests pass).
- Feature work: `feature/<short-kebab>` (e.g. `feature/cork-drop-particle`).
- Bug fix: `fix/<short-kebab>` (e.g. `fix/undo-restores-empty-bottle`).
- Refactor: `refactor/<short-kebab>`.
- Hotfix for a shipped build: `hotfix/<version>`.

Never commit directly to `main`. Use a branch and a PR.

## Commit messages — Conventional Commits

```
<type>(<scope>): <imperative summary>

<optional body — what and why, not how>

<optional footer — references to issues / PRs>
```

| Type | When |
|---|---|
| `feat` | New user-visible behaviour. |
| `fix` | Bug fix. |
| `refactor` | Code change that doesn't add behaviour or fix a bug. |
| `perf` | Performance improvement. |
| `test` | Add or fix tests only. |
| `docs` | Documentation only. |
| `build` | Build system, CI, Editor scripts, packages. |
| `chore` | Maintenance, dependency bumps, formatting. |

**Scope examples:** `domain`, `application`, `infrastructure`, `editor`, `tests`, `infra-di`, `anim`, `i18n`, `build-android`, `build-pc`.

**Summary line:** imperative, ≤ 72 chars, no trailing period, no "WIP". Bad: `added some stuff to bottle`. Good: `feat(domain): add Difficulty-based seed range for v2 generator`.

## PRs

- One logical change per PR. If the diff touches Domain and Editor for unrelated reasons, split it.
- PR title follows the commit-message convention.
- PR body: **What** (1–3 bullets), **Why** (1–2 bullets), **Test plan** (which tests you ran, which manual scenarios you exercised), **Screenshots / Clips** if there's any visual change.
- The PR description must say "closes #N" or "ref #N" for any related issue.
- A PR is mergeable only when:
  1. CI is green (compile + tests).
  2. `code-reviewer` has approved.
  3. The diff respects the Clean Architecture boundaries in `.harness/docs/code-standards.md`.

## What to commit / not commit

- ✅ Commit: `Assets/Scripts/**`, `Assets/Tests/**`, `Assets/Scenes/**`, `Assets/Resources/**`, `Assets/Settings/**`, `ProjectSettings/**`, `Packages/manifest.json`, `Packages/packages-lock.json`, every `.meta` file, the `.harness/` directory.
- 🚫 Never commit: `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Build/`, `Builds/`, `obj/`, `MemoryCaptures/`, `.idea/`, `.vs/`, `*.csproj.user`, `*.apk`, `*.aab`. These are already in `.gitignore`.
- Never force-push to `main`. Never amend a commit that's already on a remote branch.

## Local hygiene

- Rebase your feature branch onto `main` before opening a PR — `git fetch origin && git rebase origin/main`.
- If a rebase gets messy, prefer a fresh branch with a clean cherry-pick over a 30-commit interactive rebase.
- Run the test suite locally before pushing. CI exists to catch the unexpected, not to do the work for you.
