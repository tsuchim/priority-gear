# PriorityGear Agent Instructions

## Language

- User-facing reports must be Japanese.
- Agent-to-agent notes and repository documents are English unless the file is explicitly Japanese.

## Project Rules

- Do not copy AutoGear code, assets, UI resources, or decompiled behavior.
- Do not introduce telemetry, network access, or an updater in v0.1.
- Do not add fallback success paths. If the main operation fails, report failure explicitly.
- Do not widen scope from User Mode to System Mode without explicit milestone separation.
- Do not make repo-wide noisy searches unless needed; search targeted paths first.
- Keep commits coherent.
- Keep `main` and `devel` as the only permanent branches.
- Do not push directly to `main`.
- Stop on merge conflicts and report.
- Run tests before commit.
- Report changed files, tests run, and remaining risks.

## Protected Surfaces

Treat AGENTS files, runtime configuration, agent definitions, instructions, hooks, and skills as protected surfaces. Prefer structural enforcement and explicit approval over prompt-only warnings where available.
