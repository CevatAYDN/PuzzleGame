# Changelogs

One file per day. File name: `YYYY-MM-DD.md`.

Format:

```markdown
# YYYY-MM-DD

## <scope or PR title>
- <one-line summary of the change>
- <files touched if non-obvious>

## <next scope or PR title>
- ...
```

These are hand-curated, not auto-generated. Append entries at end-of-day. Use them to reconstruct the project's history if `git log` is unavailable.

## How to use

- The orchestrator (Harness) may ask any rein to append to today's file at end of work.
- A new day's file starts with the date as the H1.
- Keep entries short — one line per commit, link to the PR if there is one.

## Examples

```markdown
# 2026-06-03

## Bootstrap .harness/
- Added orchestrator + 5 reins (developer, tester, code-reviewer, game-logic-expert, unity-expert)
- Added docs/code-standards.md, docs/test-policy.md, docs/git-workflow.md
- Added shared memory with project conventions
```
