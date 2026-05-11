# Serialization And Migration Audit

This document records the current save-format and migration audit for TLE
Extended. It covers inherited TLE data plus the TSP additions.

## Compatibility Posture

TLE Extended is intended to be a drop-in replacement for Traffic Lights
Enhancement. Existing TLE intersections should load without user action, and TLE
Extended should preserve the inherited internal mod ID while the fork remains in
this compatibility mode.

TSP adds a saved per-intersection component,
`TransitSignalPrioritySettings`. It is additive: intersections without that
component behave as non-TSP intersections. TSP settings are versioned and
normalized on load.

## Current Saved TSP Fields

`TransitSignalPrioritySettings` persists:

- `m_Enabled`
- `m_AllowTrackRequests`
- `m_AllowPublicCarRequests`
- `m_RequestHorizonTicks`
- `m_MaxGreenExtensionTicks`

The runtime currently normalizes the feature to tram-only behavior:

- track requests are allowed
- public-car requests are reserved but disabled
- request horizon is clamped through policy, with legacy `120` remapped to `10`
- max green extension is clamped through policy

Future bus/public transport priority should treat the reserved public-car fields
as schema-sensitive. Turning them into active user-configurable settings may
need a save version bump rather than simply removing normalization.

## Inherited Save Components Needing Explicit Coverage

The audit found inherited `ISerializable` layouts that should be documented and
round-trip tested before compatibility-affecting edits:

- `CustomTrafficLights`
- `TrafficGroup`
- `TrafficGroupMember`
- `EdgeGroupMask`
- `SubLaneGroupMask`
- `CustomPhaseData`
- `SignalDelayData`

`TrafficLightsEnhancement.Serialization.Tests` now covers current serializer
round trips for these inherited components, plus practical legacy payload
fixtures for older `CustomTrafficLights`, `TrafficGroup`, `TrafficGroupMember`,
`EdgeGroupMask`, `CustomPhaseData`, and `GroupMask.Signal` layouts.

## Fixed: SignalDelayData Layout

`SignalDelayData.Serialize(...)` writes:

```text
TLEDataVersion.V1
m_Edge
m_OpenDelay
m_CloseDelay
m_IsEnabled
```

`SignalDelayData.Deserialize(...)` previously read the leading payload version as
the serialized entity index, shifting the rest of the stream. The current reader
now consumes the payload version first, then reads `m_Edge` through
`reader.Read(out Entity)`. The regression is covered by
`Signal_delay_data_round_trips_current_payload`.

## Risk: Migration Version Sequencing

`TLEDataMigrationSystem.OnUpdate()` currently uses an `if / else if` ladder for
version migration. That means a version `0` save appears to run only the V1
migration, not V1 plus V2 plus V5. Versions `2` through `4` also appear to reach
V5 migration and then run `MigrateCustomTrafficLights()` through a shared
non-current migration block.

This may be intentional if each migration routine is a broad validation pass,
but the sequencing is not obvious and is not covered by tests.

## Versioning Ambiguity

`TLEDataVersion` is used for more than one concern:

- global migration state
- component payload versions
- special-case component fields

For example, some components write `TLEDataVersion.Current`, while
`TrafficGroup` writes an older payload version and conditionally reads a newer
field. Future save changes should document whether a new version is a global
migration step, a component payload step, or both.

## Low-Risk Cleanup Candidate

`MigrationIssuesService` appears to keep a static affected-entity list, while
current migration UI paths use `UISystem.AffectedIntersections` directly. It may
be dead code or an unfinished ownership abstraction.

## Follow-Up Work

Track these as follow-up issues before touching inherited save behavior:

- Add serializer round-trip tests for inherited save components, starting with
  `SignalDelayData`. Done in `TrafficLightsEnhancement.Serialization.Tests`.
- Clarify and test migration version sequencing for loaded versions 0 through 5.
- Document a save-format contract that lists each saved component and buffer,
  its payload version, runtime-only fields, downgrade expectations, and
  upstream-TLE compatibility assumptions.
- Remove or wire up `MigrationIssuesService` after confirming ownership of
  migration issue state.
