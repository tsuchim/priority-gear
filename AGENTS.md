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

## Pull Request Workflow

- When a pull request is opened, wait for GitHub Copilot code review before merging. The usual wait time is about 10 minutes unless the user instructs otherwise.
- Inspect GitHub Copilot review comments one by one. For each comment, decide whether to accept it, reject it with a clear reason, or make another appropriate change.
- Resolve review comments after they have been addressed or explicitly judged non-blocking.
- Confirm GitHub Actions, CI, and other required checks are passing before merge.
- If review comments and checks are clear, normally merge pull requests into `main` using a regular merge commit. Keep `devel` aligned with `main` afterward using a normal merge, not history rewriting.
- Do not squash, rebase, force-push, or rewrite protected branch history unless the user explicitly asks for that specific operation.

## Release Workflow

- Inspect actual GitHub state and repository files before giving release or post-release instructions. Do not rely on remembered state when local files or public GitHub state can be checked.
- Preview tags matching `v*-preview.*` are intended to publish public prereleases automatically after mechanical gates pass.
- No human visual review gate is required for preview tags after restore, build, tests, packaging, checksum, and artifact inspection pass.
- Stable tags remain a separate explicit release decision.
- Elevated setup verification is recorded evidence from a real machine. Do not run elevated setup inside GitHub Actions.
- Treat the second semantic-version component as the minor version. Do not increment it casually. For ordinary fixes, previews, and release refreshes, increment the third component or the preview/release suffix instead.

## Protected Surfaces

Treat AGENTS files, runtime configuration, agent definitions, instructions, hooks, and skills as protected surfaces. Prefer structural enforcement and explicit approval over prompt-only warnings where available.
