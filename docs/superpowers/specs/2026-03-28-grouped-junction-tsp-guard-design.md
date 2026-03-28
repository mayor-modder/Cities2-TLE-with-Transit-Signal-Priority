# Grouped Junction TSP Guard Design

## Summary

This design makes Transit Signal Priority (TSP) available only on standalone intersections. Any intersection that belongs to a traffic group will keep its saved `TransitSignalPrioritySettings`, but TSP will be inactive while that group membership exists.

This is the first cleanup slice for the review findings. It intentionally removes the highest-risk interaction surface between TSP and traffic groups before we refine standalone TSP behavior.

## Terminology

- **Transit Signal Priority (TSP):** The feature that allows eligible transit vehicles to request an early phase selection or an extension of the current green.
- **Traffic group:** A set of intersections linked by `TrafficGroup` and `TrafficGroupMember`.
- **Grouped intersection:** Any intersection entity that has `TrafficGroupMember`, regardless of whether the group is currently using coordinated timing or green wave.
- **Coordinated group:** A traffic group with `TrafficGroup.m_IsCoordinated == true`.
- **Green wave:** The traffic-group feature controlled by `TrafficGroup.m_GreenWaveEnabled`.
- **Standalone intersection:** An intersection that does not have `TrafficGroupMember`.

The user-facing rule in this design is based on **traffic group membership**, not only on coordinated groups or green wave.

## Problem Statement

The current code allows TSP and traffic-group behavior to overlap. The review found several correctness problems in that overlap:

- grouped intersections can originate local TSP requests
- traffic-group propagation can translate requests incorrectly between member and leader phase spaces
- coordinated followers can sync to leader phases in ways that conflict with TSP expectations
- group-only requests can run with the wrong TSP settings

Even if some of those bugs are fixed individually, local TSP still conflicts conceptually with traffic-group timing, especially green wave. A local preemption or extension changes intersection timing in ways that undermine corridor coordination unless the whole group participates in a recovery strategy.

## Product Decision

TSP will be supported only on standalone intersections.

If an intersection is part of any traffic group:

- TSP settings remain stored on the entity
- TSP request generation is disabled
- coordinated/group-propagated TSP behavior is disabled
- the main-panel TSP controls are shown but disabled
- the UI explains that TSP is unavailable for grouped intersections

If that intersection later leaves the traffic group, its previous TSP settings become active again automatically.

## User Experience

### Main Panel

For standalone intersections:

- TSP UI behaves as it does today
- controls remain interactive

For grouped intersections:

- the TSP section remains visible
- the section shows the saved values
- all TSP controls are disabled
- a short explanatory message appears near the section

Recommended message:

`Transit Signal Priority is unavailable for intersections that are part of a traffic group. Remove this intersection from the traffic group to re-enable TSP.`

### Joining a Traffic Group

When a standalone intersection with saved TSP settings joins a traffic group:

- no TSP settings are deleted
- no TSP values are rewritten
- the intersection simply becomes TSP-inactive

### Leaving a Traffic Group

When an intersection leaves a traffic group:

- its stored TSP settings become active again immediately
- no extra migration or restoration step is required

## Runtime Behavior

### Local Request Eligibility

Local TSP request generation must short-circuit for grouped intersections before building or refreshing requests.

That means:

- no new `TransitSignalPriorityRequest` should be created for grouped intersections
- any existing runtime-only `TransitSignalPriorityRequest` should be removed when the intersection becomes grouped or when grouped-state evaluation runs

### Group Propagation

Traffic-group TSP propagation becomes effectively inert under this rule because grouped intersections are not allowed to originate TSP.

Implementation-wise, we should still add an explicit grouped-membership guard in the propagation path so the runtime behavior matches the product rule even if stale request state exists temporarily.

### Diagnostics

Diagnostics should reflect the new rule clearly. Grouped intersections should not produce active TSP request logs except for cleanup/removal of stale runtime state.

## Data and Save Behavior

### Persisted Data

`TransitSignalPrioritySettings` remains serialized exactly as before.

This design does **not** remove stored TSP settings from grouped intersections because:

- removing them would discard user intent
- restoring them later would require more state management
- inactivity is enough to enforce the new rule

### Runtime-Only Data

`TransitSignalPriorityRequest`, `TransitSignalPriorityDecisionTrace`, and group-level runtime TSP state remain transient. They should be treated as cleanup targets whenever grouped membership makes them invalid.

### Save Safety

This design reduces save churn risk by avoiding automatic mutation of stored TSP settings during group membership changes. The only expected runtime cleanup is removal of transient request/trace state that should not survive once TSP is inactive.

## Scope

### In Scope

- disable TSP runtime behavior for grouped intersections
- disable TSP controls in the UI for grouped intersections
- show an explanatory message in the UI
- preserve stored TSP settings while grouped
- ensure leaving a group re-enables the stored settings automatically
- ensure group propagation no longer activates TSP under grouped membership

### Out of Scope

- redesigning TSP to work with coordinated groups or green wave
- corridor-level or group-aware transit priority
- rebalancing standalone TSP request arbitration
- broader traffic-group performance refactors

Those remain follow-up work after the grouped-junction guard is in place.

## Implementation Notes

- The grouped-membership check should use `TrafficGroupMember` directly because that is the clearest source of truth for this product rule.
- The UI should not hide the TSP section for grouped intersections; disabled controls plus a message is the intended experience.
- The design should use existing mod terminology in UI strings and comments: `traffic group`, `green wave`, and `Transit Signal Priority`.
- Any legacy group-level toggle such as `m_TspPropagationEnabled` should be treated as inactive when all member intersections are ineligible for TSP. We do not need a new feature path to preserve it.

## Testing Strategy

We need coverage at two levels.

### Logic and Runtime Tests

- grouped intersection with saved TSP settings does not create a local TSP request
- grouped intersection removes stale runtime request state if one exists
- standalone intersection still creates requests normally
- leaving a group restores runtime eligibility without rewriting settings
- traffic-group propagation does not activate TSP when the source intersection is grouped

### UI and State Tests

- grouped intersection main panel shows the TSP section disabled
- grouped intersection shows the explanatory message
- standalone intersection shows enabled controls
- joining a group preserves saved settings values
- leaving a group exposes the same saved settings values again

## Risks

- Some users may expect grouped intersections to support TSP because the settings remain visible. The disabled-state message needs to make the rule explicit.
- Existing saves may already contain stale runtime TSP state on grouped intersections, so cleanup paths need to be deterministic.
- This design intentionally chooses correctness and clarity over flexibility. If we later want group-aware transit priority, that should be a new design rather than an incremental exception to this rule.

## Recommendation

Implement this as the first cleanup slice, then revisit the remaining standalone TSP issues. Removing TSP from grouped intersections should eliminate several of the hardest review findings and give the remaining TSP work a much simpler runtime model.
