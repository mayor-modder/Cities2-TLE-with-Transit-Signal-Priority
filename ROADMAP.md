# TLE Extended Roadmap

TLE Extended is a compatible extended fork of Traffic Lights Enhancement for
Cities: Skylines II. The project starts from the rewritten TLE codebase plus
Tram Signal Priority, and grows from there with a focus on compatibility,
maintainability, diagnostics, and broader transit-priority features.

This roadmap is maintainer-facing. The repository is public, but these notes are
not release promises.

## Guiding Principles

- Preserve drop-in compatibility with existing TLE saves and configured
  intersections wherever possible.
- Prefer opt-in behavior for changes that alter existing junction behavior.
- Version, document, and test save-format changes before users accumulate
  incompatible data.
- Keep complex logic isolated and testable outside Unity/ECS when practical.
- Make diagnostics useful enough to explain behavior in real cities, but keep
  expensive diagnostics off by default.
- Treat uncertain work as research until the implementation path is understood.
- Track concrete work as GitHub issues so future agents can continue without
  relying on chat history.

## Current Foundation

These are no longer roadmap guesses. They are the foundation future work should
preserve and extend:

- Tram Signal Priority is implemented as an opt-in, per-junction feature.
- TSP has pure policy tests, UI source tests, serialization coverage, and a
  custom state-machine regression harness for TSP-off behavior.
- Dynamic mode now documents and tests restored narrow linked-phase behavior,
  including how it interacts with TSP-selected phases.
- Bicycle phase weight is exposed in the custom phase vehicle-weight UI.
- Bus-priority groundwork includes pure policy tests, stop-aware suppression
  policy, and diagnostic-only bus approach indexing. Buses still do not affect
  signal selection.
- Maintainer docs now cover TSP architecture, diagnostics, dynamic mode,
  save-format compatibility, localization workflow, and serialization/migration
  audit notes.
- The fork is named and documented as TLE Extended while retaining the inherited
  TLE compatibility posture.

## Near-Term Decision Queue

These are the next bounded choices to resolve before larger feature expansion:

- Playtest bus approach diagnostics in real saves and collect examples where
  mixed lanes, bus-only lanes, lane changes, queues, and stop behavior differ
  from the prototype assumptions.
- Decide how much stop-relation classification is needed before buses can
  safely create requests.
- Decide the first player-facing bus control shape. The current preference is a
  separate bus control rather than renaming the existing tram control.
- Extract custom phase selection into pure logic only when a behavior change or
  larger refactor needs it; the current extraction audit does not require an
  immediate rewrite.
- Remove or retire unused inherited localization paths only after supported game
  version checks confirm they are safe to remove.
- Keep the save-format contract and localization workflow current whenever
  those surfaces change.

## Bus Priority Path

Bus priority should build on the TSP architecture, but not by immediately
flipping signals for buses. The first pass should be observability and policy:

- Pure bus-priority policy tests are in place.
- Diagnostic-only bus approach indexing is in place behind the existing
  off-by-default diagnostics option.
- Pure stop-aware suppression rules are in place for boarding, near-side stops,
  far-side stops, unknown stop relation, and queued buses.
- Next, use diagnostics to classify stop relation and lane-change behavior in
  real saves before wiring bus samples into request creation.
- Decide whether the future UI remains tram-specific, becomes a generic transit
  priority panel, or splits tram and bus controls. The current preference is
  split controls.
- Only allow bus requests to affect signals after diagnostics can explain why a
  request was or was not created.

## Longer-Term Direction

- Support broader transit priority policies across trams and buses.
- Improve pedestrian-phase and conflict-policy behavior while keeping outcomes
  predictable.
- Build better compatibility and migration tooling for users moving from TLE.
- Improve inherited TLE documentation for migrations, custom phases, traffic
  groups, and maintenance workflows.
- Prepare public release documentation only when the project is ready to be
  publicized.

## Current Non-Goals

- No Paradox Mods publication push yet.
- No release dates.
- No broad marketing page.
- No large unrelated cleanup without an issue and a focused commit.
- No bus signal control until bus diagnostics and suppression rules are
  understood in real saves.
