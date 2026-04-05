# Mod Build Workflow Guardrails Design

## Goal

Prevent a repeat of the "verification build succeeded, but the installed mod was not actually playable" mistake by making verification and deploy two explicit workflows with clear names.

## Decision

Keep compile-only verification builds available during development, but require a distinct full deploy command for any end-of-task playable build.

## Workflow

- `scripts/Verify-Mod.ps1`
  - Runs a compile-only `dotnet build` with `DisablePostProcessors=true`
  - Gives fast confidence while iterating
  - Prints an explicit warning that it does not produce a playable installed mod

- `scripts/Deploy-Mod.ps1`
  - Runs the full postprocessed build that copies the mod into the Cities II local mods folder
  - Verifies that the deployed folder contains the required managed, native, and UI files
  - Warns if Cities II is currently running, because a restart is still required to load the rebuilt package in memory

## Documentation

- Update `BUILD.md` so the repo has a fork-specific "verify vs deploy" section
- Update `README.md` build notes to call out that `DisablePostProcessors=true` is compile-only and not a playable install path

## Why This Approach

- It is repo-local and does not depend on Codex or MCP server state
- The command names are explicit enough to prevent accidental misuse
- It matches the desired workflow: fast verification while working, full playable deploy when done

## Non-Goals

- Changing the Cities II modding toolchain
- Depending on a workbench-only hook for correctness
- Replacing local judgment with a hard failure whenever the game is running
