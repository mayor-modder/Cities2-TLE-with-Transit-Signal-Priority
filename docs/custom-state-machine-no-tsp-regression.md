# Custom State Machine No-TSP Regression Harness

This document captures the regression coverage plan for custom signal state
transitions when Tram Signal Priority (TSP) is disabled or absent.

## Purpose

TSP adds overloads to the custom signal state machine so selected intersections
can hold or override custom phases for tram requests. The no-request path must
continue to behave like the inherited TLE state machine.

The regression to prove is:

```text
new overload called with hasTspRequest=false == legacy overload
```

## Current Limitation

The existing xUnit project targets `net8.0` and references only the pure
`TrafficLightsEnhancement.Logic` project. `CustomStateMachine` lives in the
`net48` mod assembly and depends on Cities II / Unity ECS types, including:

- `Game.Net.TrafficLights`
- `Unity.Entities.DynamicBuffer<T>`
- `CustomTrafficLights`
- `CustomPhaseData`
- the local Cities II modding toolchain and game assemblies

That makes a direct regression test unsuitable for the current pure logic test
project. It should be covered by a Unity/ECS-capable harness instead of forcing
game assemblies into the pure test suite.

## Regression Oracle

For each scenario, construct equivalent `TrafficLights`,
`CustomTrafficLights`, and `DynamicBuffer<CustomPhaseData>` state, then compare
the legacy and TSP-aware overloads.

### State Update

Compare:

```csharp
CustomStateMachine.UpdateTrafficLightState(
    ref trafficLights,
    ref customTrafficLights,
    phases);
```

with:

```csharp
CustomStateMachine.UpdateTrafficLightState(
    ref trafficLights,
    ref customTrafficLights,
    phases,
    phases,
    default,
    false,
    default,
    out tspSelection);
```

Expected:

- the return value is identical
- `TrafficLights` fields are identical
- `CustomTrafficLights` fields are identical
- phase buffer fields are identical
- `tspSelection.Applied` is `false`

### Next-Group Selection

Compare:

```csharp
CustomStateMachine.GetNextSignalGroup(
    currentGroup,
    phases,
    customTrafficLights,
    out linkedWithNext);
```

with:

```csharp
CustomStateMachine.GetNextSignalGroup(
    currentGroup,
    phases,
    customTrafficLights,
    out linkedWithNext,
    out tspSelection,
    false,
    default);
```

Expected:

- selected group is identical
- `linkedWithNext` is identical
- `tspSelection.Applied` is `false`

## Scenario Matrix

The harness should cover:

- empty phase buffer returns group `0`
- manual signal group overrides selection
- fixed timed sequential mode selects the next group
- fixed timed smart-selection mode preserves existing best-step and restart
  behavior
- dynamic mode selects the next phase with positive minimum duration
- dynamic mode skips zero-minimum phases without demand
- dynamic mode selects a zero-minimum phase with demand or priority
- `None`, `Extending`, and `Extended` transition to `Beginning`
- `Beginning` transitions to `Ongoing`
- `Ongoing` before minimum duration does not transition
- `Ongoing` at maximum duration transitions to `Ending`
- `EndPhasePrematurely` transitions to `Ending`
- `Ending` transitions to `Changing`
- `Changing` transitions to `Beginning`

## Source Invariant

The code currently preserves the no-TSP path through delegation:

- legacy `UpdateTrafficLightState(...)` delegates to the TSP-aware overload with
  `hasTspRequest=false`
- legacy `GetNextSignalGroup(...)` delegates to the TSP-aware overload with
  `hasTspRequest=false`
- `ApplyTspOverride(...)` returns the base group immediately when there is no
  TSP request

The harness above is still needed because the behavior depends on Unity ECS
state and mutable buffers that are not covered by the pure xUnit project.

## Recommended Command Shape

Run from a machine with Cities II and the local modding toolchain installed:

```powershell
dotnet build TrafficLightsEnhancement\TrafficLightsEnhancement.csproj --no-restore
dotnet test <UnityEcsHarnessProject>.csproj --no-restore
```

## Follow-Up Work

Track the harness as implementation work rather than merging it into the pure
logic project. A separate follow-up should also evaluate whether the base custom
phase selection logic can be extracted into pure DTOs without creating churn in
the production state machine.
