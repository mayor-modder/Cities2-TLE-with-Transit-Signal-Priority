# TSP Diagnostics Cost And Trace Audit

This document records the diagnostics audit for Tram Signal Priority (TSP).

## Summary

The selected-intersection diagnostics UI and JSONL trace writer are opt-in and
off by default. When enabled, they are useful for live troubleshooting, but they
also make selected-panel refresh broader than normal gameplay needs. The most
important always-on cost is not the JSONL writer; it is the tram approach index,
which is built whenever any TSP settings component exists.

## UI Diagnostics Gating

The user-facing setting is `Settings.m_ShowTramSignalPriorityDiagnostics`.
Defaults:

- `false` in settings defaults
- diagnostics panel is only built when the option is enabled
- selected-panel auto-refresh for diagnostics is only active when the option is
  enabled

When enabled, diagnostics are built for the selected main-panel entity. If the
selected intersection has no TSP settings component, the UI still builds a
disabled/default diagnostics view. That is useful when debugging why a selected
intersection has no request, but it can also produce trace churn for non-TSP
intersections.

## JSONL Trace Behavior

The trace file is a debugging artifact, not gameplay state.

- File name: `C2VM.TrafficLightsEnhancement.TspDiagnostics.jsonl`
- Location: `Application.persistentDataPath`
- Write path: selected-panel diagnostics refresh
- Rotation threshold: 5 MB
- Retention: newest 3 rotated files plus the active file
- Deduplication: per selected entity, based on the last summary signature

Trace writes are lock-protected, exception-handled, and bounded by rotation plus
retention. They are still synchronous UI-triggered file I/O, so diagnostics
should remain opt-in.

## Runtime Debug Components

When an active TSP request is resolved, the simulation writes
`TransitSignalPriorityRuntimeDebugInfo` independently of the UI diagnostics
option. That component mostly captures values already computed during request
resolution, and it keeps selected-intersection debugging available without
rerunning runtime detection in the UI layer.

This is acceptable for now, but it should be profiled before increasing TSP
scope or enabling diagnostics by default.

## Tram Approach Index

`PatchedTrafficLightSystem.OnUpdate()` builds `TramApproachIndex` when the
`TransitSignalPrioritySettings` query is non-empty. The policy is intentionally
simple:

```text
any persisted TSP settings component -> scan rail transit lanes for the tick
```

Disabling TSP from the UI removes the settings component, which avoids the scan
for users who have no TSP-enabled intersections. However, the query does not
filter by `m_Enabled` or by runtime eligibility. If a disabled or stale settings
component exists, the approach index can still be built.

## Follow-Up Work

Useful follow-ups:

- Gate trace writes to meaningful TSP activity, or split "show diagnostics" and
  "write JSONL trace" into separate settings.
- Narrow selected-panel diagnostics so non-TSP intersections only trace when an
  explicit selected-debug mode is active.
- Optimize `ShouldBuildApproachIndex(...)` so rail transit scans require at
  least one enabled runtime-eligible TSP setting, or document the current
  component-existence policy as the intended behavior.
- Profile `TransitSignalPriorityRuntimeDebugInfo` writes before adding bus
  priority or broader public transport priority.
