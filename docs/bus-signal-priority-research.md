# Bus Signal Priority Research

This document records initial research for extending Tram Signal Priority (TSP)
toward bus signal priority.

## Current State

The current feature is deliberately tram-only.

- `TspSource.PublicCar` exists as a reserved source.
- `m_AllowPublicCarRequests` exists in settings and serialization.
- Runtime normalization and UI toggling keep public-car requests disabled.
- Pure decision tests currently assert that public-car requests are ignored.

The tram path builds `TramApproachIndex` from rail public transport vehicles
using `PublicTransport`, `TrainNavigation`, and `TrainCurrentLane`. Fresh
request production scans signaled sublanes, resolves source lanes, and only
builds requests when the resolved approach lane is a tram track.

## Reusable Pieces

Bus priority can reuse much of the TSP pipeline:

- saved settings component shape
- latched request component
- request expiry policy
- selected-intersection diagnostics and trace structure
- signal group hold/override application
- exclusive pedestrian phase protection
- custom phase integration

The lane and signal group mapping also has useful inherited support. Bus-only
lanes are represented through `CarLaneFlags.PublicOnly`, and custom phase masks
already track public-car lane groups separately from general car lanes.

## Missing Pieces

Bus detection needs a road-vehicle approach index, not just public-only lane
detection. A bus in a mixed car lane should request that lane's signal group,
while a bus-only lane should use the public-car lane mask where custom phases
split it.

Likely ECS data sources to investigate:

- `PublicTransport`
- `PassengerTransport`
- `CarCurrentLane`
- `CarNavigation`
- `CarNavigationLane`
- `Moving`
- `PrefabRef`
- `PublicTransportVehicleData.m_TransportType == Bus`

The current `ExtraTypeHandle` does not expose the road-vehicle state needed for
bus detection.

Pure policy also needs source-generalization. Today request construction,
request combination, phase scoring, latching, current-group hold, aggressive
preemption, and overrides are effectively track-only.

## Edge Cases

Near-side stops are the biggest policy risk. A bus approaching a stop before the
signal should not request or hold green too early if it is about to board
passengers. The tram index suppresses boarding samples but allows
`Arriving`/`RequireStop`; buses may need stricter stop-aware handling.

Mixed lanes require vehicle-level detection. A lane marked for regular cars can
still carry a bus, and bus priority should follow the actual bus lane/current
route, not only the lane type.

Lane changes matter. `CarCurrentLane` can include both current and change-lane
state, and choosing the wrong lane near an intersection could select the wrong
signal group.

Congestion also matters. A stopped bus far behind a queue may not deserve
priority until it is close enough, latched, or otherwise confirmed to benefit
from priority.

## MVP Recommendation

Use a soft bus-priority MVP:

- enable bus requests only behind an explicit setting
- let tram requests outrank bus requests
- allow buses to hold an already-serving green
- allow buses to select the target group at normal transition points
- do not use tram-style aggressive minimum-green shortening for buses in the
  first version

That keeps bus priority useful while avoiding the most disruptive cases until
stop and lane-change behavior is better understood.

## Staged Plan

1. Add pure policy tests for `PublicCar` eligibility and source ordering.
2. Prototype a diagnostic-only bus approach index that reports bus lane hits but
   does not change signals.
3. Integrate bus fresh request production from car-lane bus samples.
4. Add UI/settings surface, either by renaming the feature to broader "Transit
   Signal Priority" or by adding separate tram and bus toggles.
5. Add stop-aware suppression, lane-change handling, mixed-lane regression
   cases, and grouped-intersection semantics.

## Follow-Up Work

Suggested follow-up issues:

- Add pure bus priority policy tests.
- Prototype bus approach index diagnostics.
- Implement bus request production from car-lane bus samples.
- Define stop-aware bus suppression rules.
- Rename or split the current TSP UI for tram vs bus priority.
