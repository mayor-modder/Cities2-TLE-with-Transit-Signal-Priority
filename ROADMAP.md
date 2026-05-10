# TLE Extended Roadmap

TLE Extended is a compatible extended fork of Traffic Lights Enhancement for Cities: Skylines II. The project starts from TLE plus Tram Signal Priority and grows from there with a focus on compatibility, maintainability, documentation, diagnostics, and transit-priority features.

This roadmap is maintainer-facing. The repository is public, but these notes are not release promises.

## Principles

- Preserve drop-in compatibility with existing TLE saves and configured intersections wherever possible.
- Prefer opt-in behavior for changes that alter existing junction behavior.
- Version and test save-format changes.
- Keep complex logic isolated and testable outside Unity/ECS when practical.
- Keep diagnostics useful, off by default when expensive, and safe for long play sessions.
- Track uncertain work as research until the implementation path is understood.

## Short Term

- Stabilize and harden Tram Signal Priority.
- Add regression coverage for TSP-off custom signal behavior.
- Document TSP architecture and data flow.
- Audit diagnostics cost and trace-file behavior.
- Rename and document the fork as TLE Extended while preserving compatibility.
- Track repeated review findings as issues instead of chat-only notes.

## Medium Term

- Add in-game descriptions for inherited mod options.
- Improve maintainer documentation for serialization, migrations, dynamic mode, custom phases, and traffic groups.
- Research bus signal priority using the TSP architecture as the starting point.
- Improve test coverage around pure logic, UI bindings, and save-format behavior.

## Long Term

- Support broader transit priority policies across trams and buses.
- Improve pedestrian-phase and conflict-policy behavior while keeping outcomes predictable.
- Build better compatibility and migration tooling for users moving from TLE.
- Prepare public release documentation only when the project is ready to be publicized.

## Current Non-Goals

- No Paradox Mods publication push yet.
- No release dates.
- No broad marketing page.
- No large unrelated cleanup without an issue and a focused commit.
