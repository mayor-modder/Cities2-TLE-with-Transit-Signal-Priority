# Custom Phase Selection Extraction

This note resolves the research question from issue #11: whether the custom
phase selection logic should be extracted into pure, Unity-free code.

## Recommendation

Extract the base no-TSP custom phase selection logic, but do not extract the
full custom phase transition loop yet.

The useful boundary is the logic that chooses the next custom signal group:
manual override, empty phase buffer fallback, fixed timed sequential selection,
fixed timed smart selection, dynamic demand scanning, and the existing
best-step scoring. Leave timer mutation, phase-state transitions, TSP override
application, and linked-phase redesign inside the production ECS layer for now.

This is worth doing because the current no-TSP regression suite needs a
synthetic `DynamicBuffer<CustomPhaseData>` harness only to reach production
selection behavior under `dotnet test`. A small pure selector would make future
custom-phase, tram-priority, and bus-priority work easier to test without
expanding that unsafe harness.

## Minimal DTOs

`CustomPhaseSelectionInput` should contain:

- `CurrentGroup`
- `Mode`
- `SmartSelectionEnabled`
- `ManualSignalGroup`
- `IReadOnlyList<CustomPhaseSelectionPhase>`

`CustomPhaseSelectionPhase` should contain only the values used by selection:

- `MinimumDuration`
- `Priority`
- `WeightedLaneOccupied`
- `AverageCarFlow`
- `WeightedWaiting`
- `WaitFlowBalance`
- `CurrentFlow`
- `CurrentWait`
- `ChangeMetric`

`CustomPhaseSelectionResult` should contain:

- `SelectedGroup`
- `LinkedWithNextPhase`
- `ShouldRestartCurrent`

`LinkedWithNextPhase` should remain available for parity because linked-phase
behavior remains in `CustomStateMachine`. It should not be redesigned as part
of this extraction.

## Extraction Boundary

Move the base selection decision into `TrafficLightsEnhancement.Logic`, in a
new custom-phase namespace or folder next to the existing `Tsp` pure logic.
The production `CustomStateMachine.GetNextSignalGroup(...)` should map
`DynamicBuffer<CustomPhaseData>` into DTOs, call the pure selector, and then
continue through the existing TSP override path.

Keep these behaviors in `CustomStateMachine` for this phase:

- `UpdateTrafficLightState(...)` timer and state mutation
- `ApplyTspOverride(...)`
- minimum/maximum phase duration enforcement during active phases
- linked-phase behavior changes

## Production Risk

The risk is low-to-medium if the extraction stays limited to base selection.
The main hazards are preserving:

- 1-based signal group numbers
- current-group fallback when `CurrentGroup <= 0`
- invalid manual group fallback
- fixed smart-selection behavior, including restart-current cases
- dynamic zero-minimum phase behavior
- `float.NaN` behavior in scoring
- exact weighting semantics from `CustomPhaseData.WeightedLaneOccupied()`

The risk becomes much higher if this work also moves transition timing,
mutable phase counters, or linked-phase behavior. Those should be handled in
separate issues.

## Test-First Implementation Plan

1. Add pure tests for current parity cases:
   - empty phase list
   - manual override
   - fixed timed sequential selection
   - fixed timed smart selection
   - dynamic positive-minimum selection
   - dynamic zero-minimum phase skipped without demand
   - dynamic zero-minimum phase selected with demand

2. Add pure edge tests:
   - invalid manual group falls back to normal selection
   - `CurrentGroup` of `0` clamps to the first valid phase
   - smart selection can restart the current phase when the current phase is
     still the best choice
   - `float.NaN` scores do not become accidental winners
   - all-skippable dynamic phases have a deterministic fallback

3. Implement `CustomPhaseSelectionEngine` in `TrafficLightsEnhancement.Logic`.

4. Update `CustomStateMachine.GetNextSignalGroup(...)` to map ECS phase data
   into DTOs and call the pure selector.

5. Run both suites:
   - `dotnet test TrafficLightsEnhancement.Tests\TrafficLightsEnhancement.Tests.csproj --no-restore`
   - `dotnet test TrafficLightsEnhancement.Ecs.Tests\TrafficLightsEnhancement.Ecs.Tests.csproj --no-restore`

6. Keep the existing ECS no-TSP regression tests after extraction. They should
   become integration parity tests rather than the only way to test selection.
