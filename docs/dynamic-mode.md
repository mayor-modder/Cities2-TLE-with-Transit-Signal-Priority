# Dynamic Mode Behavior

This document explains the inherited dynamic custom phase behavior after the
upstream rewrite and records interactions that matter for TSP.

## Ownership

Dynamic mode is implemented in `CustomStateMachine` and configured by
`CustomPhaseData` plus `CustomTrafficLights`.

Key responsibilities:

- `CustomPhaseData` stores per-phase timing, demand, priority, and vehicle
  weight fields.
- `CustomTrafficLights` stores the selected custom traffic light mode.
- `CustomStateMachine` refreshes demand/flow metrics, advances the current
  custom phase state, and selects the next signal group.

## Phase Order

Dynamic mode cycles through user-defined custom phases in order. Next-phase
selection scans forward from the current phase and wraps around the phase
buffer.

Eligibility:

- phases with `m_MinimumDuration > 0` are always eligible
- phases with `m_MinimumDuration == 0` are skippable
- zero-minimum phases run when they have demand, currently priority or occupied
  weighted lanes
- if every phase is skippable and has no demand, dynamic mode falls back to the
  next sequential phase so the state machine does not stall

This is separate from fixed timed mode's optional smart phase selection.

## Phase Duration

Each phase is bounded by its configured minimum and maximum duration.

- Before or at minimum duration, the current phase does not end.
- At or beyond maximum duration, the current phase ends.
- Between those bounds, dynamic mode uses the selected phase-change metric plus
  measured flow and wait values to decide whether the phase may end.

The target duration calculation is:

```text
10 * (average car flow + track lane occupied * 0.5) * target duration multiplier
```

The target is not a hard duration. It delays ending a still-prioritized phase
until the timer passes the target and the low-flow counter accumulates. A
higher-priority competing phase can still end the current phase sooner through
the low-priority counter.

## Phase Change Metrics

`CustomPhaseData.StepChangeMetric` controls the mid-range change policy:

- `Default`: change when flow is lower than weighted wait
- `FirstFlow`: change when flow exists
- `FirstWait`: change when waiting demand exists
- `NoFlow`: change when current flow is absent
- `NoWait`: change when waiting demand elsewhere is absent

Dynamic wait is based on the maximum waiting value from other phases, multiplied
by `m_WaitFlowBalance`.

## Demand And Flow Refresh

Before the state machine advances, `CalculatePriority(...)` refreshes lane
demand and priority:

- resets occupied-lane counters
- reads lane petitioners and priorities
- counts cars, public cars, tracks, pedestrians, and bicycle lanes per signal
  group
- calculates weighted waiting and flow/wait ratios

`CalculateFlow(...)` samples lane flow history only for lanes served by the
current signal group.

## UI Persistence

The UI exposes dynamic settings for phase change mode, wait sensitivity,
minimum duration, maximum duration, target duration multiplier, interval
exponent, vehicle weights, smoothing factor, and live statistics.

The C# update handler clearly persists:

- mode
- minimum and maximum duration
- target duration multiplier
- interval exponent
- linked/end-prematurely flags
- change metric
- wait-flow balance

Min/max edits clamp each other so `minimum <= maximum`, and group cycle length
is recalculated when either duration changes.

## TSP Interaction

`PatchedTrafficLightSystem` resolves a local TSP request before custom phase
simulation and passes it into `CustomStateMachine.UpdateTrafficLightState(...)`.

During an ongoing custom phase:

- if the current group already serves the TSP request, TSP can hold that group
  up to the max green extension limit
- if the phase is ending or selecting the next group, dynamic mode first
  computes its base sequential/demand-aware group, then TSP may override that
  selected group to the requested tram-serving group
- if the request targets the current phase, next-phase override is skipped and
  current-phase holding handles it

Exclusive pedestrian protection applies to custom phases. When exclusive
pedestrian mode is enabled and the active ongoing group is a pedestrian phase,
TSP may hold that same group when compatible but should not preempt away to a
different target group.

## Coordinated Traffic Groups

Coordinated followers do not run independent phase timing. They mirror the
leader through `SyncSignalGroupWithLeader(...)`.

- Without green wave, followers copy leader phase, state, and timers directly.
- With green wave, followers map leader phase and next phase through
  `m_PhaseOffset` and subtract signal delay from timers.
- The UI displays leader phase settings read-only for coordinated followers.

Current TSP runtime rejects non-leader grouped intersections. Future group-wide
TSP needs explicit leader/follower semantics.

## Uncertainties

Follow-up research should confirm:

- whether timer fields are gameplay seconds, simulation ticks, or signal ticks
- whether dynamic vehicle weight controls persist correctly from the UI
- how linked phase behavior is intended to work after the rewrite
- whether follower-local TSP requests should route to the leader or remain
  ignored
