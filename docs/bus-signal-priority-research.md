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
- `PublicTransport.m_State` with `Boarding`, `Arriving`, and `RequireStop`
- `PassengerTransport`
- `CarCurrentLane`
- `CarNavigation`
- `CarNavigationLane`
- `Moving`
- `PrefabRef`
- `PublicTransportVehicleData.m_TransportType == Bus`
- `CurrentRoute`
- route stop entities with `BusStop` and `TransportStop`
- route/vehicle buffers such as `RouteWaypoint`, `RouteVehicle`,
  `RouteLane`, and `VehicleTiming`

`ExtraTypeHandle` now exposes the initial road-vehicle state needed for
diagnostic-only bus detection: `PassengerTransport`, `CarCurrentLane`,
`CarNavigation`, `CarNavigationLane`, `Moving`, and
`PublicTransportVehicleData`.

Pure policy also needs source-generalization. Today request construction,
request combination, phase scoring, latching, current-group hold, aggressive
preemption, and overrides are effectively track-only.

## Stop-Aware Suppression Policy

Bus priority should be stricter than tram priority around stops. A tram with
`Arriving` or `RequireStop` can still be worth detecting because tram stops are
often integrated with the track approach. A bus approaching a near-side stop may
board passengers before the signal, so requesting green before boarding would
hold cross traffic for no benefit.

Available ECS data from `Game.dll` reflection:

- `Game.Vehicles.PublicTransport` has `m_State`, `m_TargetRequest`,
  `m_DepartureFrame`, `m_PathElementTime`, `m_MaxBoardingDistance`, and
  `m_MinWaitingDistance`.
- `Game.Vehicles.PublicTransportFlags` includes `Boarding`, `Arriving`, and
  `RequireStop`.
- `Game.Prefabs.PublicTransportVehicleData.m_TransportType` identifies buses
  with `TransportType.Bus`.
- `Game.Vehicles.CarCurrentLane` exposes current lane, change lane, curve
  position, lane flags, lane position, distance, and change progress.
- `Game.Routes.TransportStop` carries stop flags/loading data, and bus stops can
  be identified by the marker component `Game.Routes.BusStop`.
- Route context is available through route components/buffers such as
  `CurrentRoute`, `RouteWaypoint`, `RouteVehicle`, `RouteLane`, and
  `VehicleTiming`.

Pure stop suppression is now captured by
`BusPrioritySuppressionPolicy.EvaluateStopSuppression(...)`:

- `Boarding` always suppresses bus priority.
- `Arriving` or `RequireStop` suppresses priority for a known near-side stop
  before the signal.
- `Arriving` or `RequireStop` does not suppress priority for a known far-side
  stop after the signal; helping the bus cross the junction can still be useful.
- `Arriving` or `RequireStop` with unknown stop relation suppresses
  conservatively until diagnostics can classify the stop.
- A queued bus with no stop flags is not stop-suppressed by this policy. Runtime
  detection may still require movement/position thresholds before creating a
  request, but queueing is not the same as boarding.

Runtime implementation should classify stop relation before creating bus
requests:

- **Near-side stop:** suppress while `Arriving`, `RequireStop`, or `Boarding`.
- **Far-side stop:** allow approach priority unless the bus is actually
  `Boarding`.
- **Stopped behind queue:** do not suppress solely because the bus is stopped;
  use distance/curve thresholds and request expiry to decide whether it is close
  enough to benefit.
- **Unknown stop relation:** suppress stop-bound buses and report the unknown
  relation in diagnostics.

## Edge Cases

## Diagnostic Prototype

The first runtime prototype is diagnostic-only. When the off-by-default TSP
diagnostics option is enabled, `BusApproachIndex` scans public-transport road
vehicles with `PublicTransportVehicleData.m_TransportType == Bus` and records
current/change-lane samples. The selected junction diagnostics can now report:

- indexed bus lane count
- whether a hit came from the signaled lane, resolved approach lane, or
  connected approach fallback
- bus-only versus mixed lane structure via `CarLaneFlags.PublicOnly`
- lane-change progress, speed, public-transport state, and vehicle lane flags

This intentionally does not create `TransitSignalPriorityRequest` values,
select signal groups, or otherwise affect traffic lights. It exists so bus
detection can be playtested against real saves before bus priority changes
signal behavior.

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

## Naming Decision

Keep the current user-facing UI as **Tram Signal Priority** until bus priority
is actually wired into runtime detection and settings.

The code can keep internal `TransitSignalPriority*` names because the saved
component shape and pure policy layer are intended to support more than one
transit source over time. The visible panel, diagnostics headings, and mod
option labels should remain tram-specific while only trams can affect signals.
This avoids promising bus behavior before it exists and preserves existing
tram-only saved settings compatibility.

When bus priority is ready for player testing, prefer separate tram and bus
controls over a single renamed "Transit Signal Priority" toggle. Separate
controls make the behavior easier to explain, keep existing tram settings
stable, and let buses stay disabled by default while diagnostics mature.

Localization impact: keep new base strings in `Locale.json` first. Do not
rewrite non-English locale files by hand for this rename/split; let the normal
translation workflow handle new strings after the English UI is stable.

## Staged Plan

1. Add pure policy tests for `PublicCar` eligibility and source ordering.
2. Prototype a diagnostic-only bus approach index that reports bus lane hits but
   does not change signals. (Done.)
3. Integrate bus fresh request production from car-lane bus samples.
4. Add a separate bus settings/control surface while keeping the existing tram
   labels and saved settings stable.
5. Add stop-aware suppression, lane-change handling, mixed-lane regression
   cases, and grouped-intersection semantics.

## Follow-Up Work

Suggested follow-up issues:

- Add pure bus priority policy tests.
- Prototype bus approach index diagnostics.
- Implement bus request production from car-lane bus samples.
- Define stop-aware bus suppression rules.
- Rename or split the current TSP UI for tram vs bus priority.
