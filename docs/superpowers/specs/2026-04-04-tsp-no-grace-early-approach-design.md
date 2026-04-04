# TSP No-Grace Early Approach Design

> Refines `docs/superpowers/specs/2026-03-29-tsp-early-approach-detection-design.md` by removing post-clear latching and making tram requests early enough to avoid braking at simple crossings.

## Goal

Make Transit Signal Priority feel immediate and visible for trams by:
- removing the post-clear roadway-red grace after a tram has already cleared the intersection
- detecting approaching tram requests early enough that the tram should not need to stop or noticeably slow in the clean single-crossing repro

## Scope

This design changes only local TSP request timing and lifecycle behavior.

It does:
- remove the existing local-request release grace
- add an earlier request path for eligible approaching transit vehicles
- preserve the existing petitioner-based request path as a fallback
- keep the current phase override and target-group selection model

It does not:
- add new user-facing settings
- redesign grouped propagation
- change non-TSP light timing outside an active request
- retune signal change-state durations

## Problem Summary

The current local TSP runtime has two behaviors that make tram priority feel weak in-game:

1. A fresh request is normally created only once `LaneSignal.m_Petitioner` is already populated. In practice this is late enough that a tram may already be at the stop line and forced to wait through the light-change sequence.
2. After the fresh request disappears, the preemption policy intentionally keeps the request alive for a short release grace when the requested group is currently green. This keeps the roadway red after the tram has already cleared the crossing.

In the simple repro intersection, those two behaviors show up as:
- the tram stopping on every pass
- the roadway remaining red for about six extra in-game seconds after the tram clears

## Product Decision

TSP should favor transit continuity over post-clear buffering in this scenario.

For V1 of this refinement:
- a tram that is clearly approaching the crossing should be able to request priority before it reaches the stop line
- once no fresh request remains, the local request should expire normally without an extra hold period just because the requested phase is currently active

This is intentionally a gameplay-first decision for dedicated tram crossings.

## Design

### 1. Remove local release grace

The local TSP latch should no longer clamp an active same-group request to a fixed release window after the fresh request disappears.

When there is no fresh request:
- if the existing latched request has remaining lifetime, it should simply decrement by one tick
- no special-case shortening or holding should be applied just because the requested target matches the current signal group

This removes the deliberate post-clear roadway-red delay while preserving the normal per-tick request horizon countdown.

### 2. Add predictive track approach detection

Before the petitioner-based path runs, the runtime should try to detect an eligible approaching transit vehicle early enough to request the crossing before the tram reaches the stop line.

The early path should:
- inspect the lane/entity context around a controlled track approach
- identify an approaching transit vehicle that is moving toward the junction
- produce the same `TransitSignalPriorityRequest` shape as the existing local path

This path should prioritize track-based transit first, since the user-visible problem is most obvious on tram crossings with dedicated right-of-way.

### 3. Keep petitioner detection as fallback

If predictive detection cannot confidently produce a request, the runtime should continue using the existing petitioner-based path.

This keeps the change safe:
- early detection improves the best case
- the current stop-line behavior remains the fallback instead of disappearing

### 4. Keep downstream selection logic unchanged

Once a request exists, the rest of the TSP pipeline should remain unchanged:
- same target-signal-group selection
- same phase override selection
- same minimum-green reduction for preemption toward another group
- same extension rules when the requested group is already serving the request

This change is about request timing and release behavior, not about inventing a new signal-timing model.

## Architecture

### Runtime Layer

The implementation belongs primarily in `TrafficLightsEnhancement/Systems/TrafficLightSystems/Simulation/TransitSignalPriorityRuntime.cs`.

That file should be reshaped so `TryBuildFreshRequest` clearly orchestrates:
1. predictive early detection
2. petitioner-based fallback detection

The helper boundaries should make it obvious which code is pure lane-type/request construction and which code depends on ECS/simulation state.

### Logic Layer

The latch behavior change belongs in `TrafficLightsEnhancement.Logic/Tsp/TspPreemptionPolicy.cs`.

That policy should become simpler:
- refresh a fresh request to the configured horizon
- otherwise decrement any existing request until expiry
- do not inject a special release grace for the current group

## Data Flow

1. Junction is confirmed locally TSP-eligible.
2. Runtime tries predictive early approach detection for the eligible transit approach.
3. If no early request is found, runtime falls back to petitioner-based detection.
4. Fresh request, if any, is merged with the existing latched request through the simplified preemption policy.
5. Existing TSP phase-selection logic consumes the active request unchanged.

## Guardrails

- Early detection must return no request when it cannot confidently identify an eligible approaching transit vehicle.
- A detected vehicle must still map to a valid target signal group or no request is emitted.
- The fallback petitioner path must remain intact.
- The no-grace latch change must not remove normal request decay; it only removes the same-group release clamp.

## Testing

Add or update tests for:
- existing local request decay no longer clamps to a six-tick same-group release grace
- an approaching track request can be produced before petitioner-only detection would trigger
- petitioner-based local detection still works when early detection does not apply
- same-group extension behavior still works when a fresh request is present

If ECS-heavy approach detection is hard to cover directly, factor the eligibility decision into a helper that can be unit-tested with focused inputs.

## Risks

- If early detection is too aggressive, trams could request priority farther upstream than intended.
- If early detection is too conservative, the tram may still brake and the feature will feel unchanged.
- Removing the release grace may slightly shorten the protection window around the end of a transit crossing, so the verification pass should pay close attention to whether road traffic resumes too abruptly.

## Recommendation

Implement this as a narrow behavioral refinement:
- remove the no-longer-desired same-group release grace
- add predictive early approach detection for local transit requests
- keep all later TSP decision logic intact

That gives the smallest code change set that matches the requested gameplay outcome: the tram should stay moving, and the road should recover as soon as the active request is truly gone.
