# AGENTS.md

## Context

- Unity game jam project.
- Development time is very limited (~20 hours).
- Theme: "반복".
- Two developers work simultaneously through GitHub.

## Priority

Prioritize:

1. Working core gameplay
2. Complete start → play → end flow
3. Bug fixes and stability
4. Feedback/polish
5. Extra features

Prefer the simplest reliable implementation over complex architecture.

## Development rules

- Inspect relevant existing code before modifying it.
- Reuse existing systems instead of duplicating them.
- Modify only files needed for the requested task.
- Do not perform unrelated refactoring.
- Do not add packages or plugins unless explicitly necessary.
- Expose gameplay tuning values with SerializeField when appropriate.
- Never claim Unity Editor testing was performed if it was not.

## Git / Unity safety

- Three developers may be working at the same time.
- Minimize changes to shared .unity, .prefab, .asset and ProjectSettings files.
- Do not rewrite another developer's work.
- Preserve Unity .meta files when creating, moving, renaming, or deleting assets.
- Never use force push, reset --hard, rebase, or delete branches unless explicitly requested.
- If a requested change is likely to conflict with another developer's work, report it before making a large change.

## Completion

After implementation:

- Check for obvious compile errors.
- Review the changed files for unintended changes.
- Briefly report what changed and what must be tested in Unity.
