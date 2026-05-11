# Tram Signal Priority Architecture

This document maps the Tram Signal Priority (TSP) implementation for future maintainers and coding agents. TSP is split between a pure C# decision layer, Unity/ECS runtime integration, and UI diagnostics. The pure layer owns policy and testable decisions; the Unity layer turns game state into requests and applies those requests to traffic-light state machines.

## Quick Map

| Area | Main Files | Responsibility |
| --- | --- | --- |
| Pure logic | [`TrafficLightsEnhancement.Logic/Tsp`](../TrafficLightsEnhancement.Logic/Tsp) | Settings normalization, request DTOs, request selection, preemption policy, override policy, display formatting, and unit-tested helpers. |
| Saved ECS settings | [`TransitSignalPrioritySettings.cs`](../TrafficLightsEnhancement/Components/TransitSignalPrioritySettings.cs) | Per-intersection saved TSP configuration. Converts to pure logic settings with `ToLogicSettings()`. |
| Runtime ECS state | [`TransitSignalPriorityRequest.cs`](../TrafficLightsEnhancement/Components/TransitSignalPriorityRequest.cs) | Latched per-intersection request state used across simulation ticks. |
| Runtime diagnostics | [`TransitSignalPriorityRuntimeDebugInfo.cs`](../TrafficLightsEnhancement/Components/TransitSignalPriorityRuntimeDebugInfo.cs), [`TransitSignalPriorityDecisionTrace.cs`](../TrafficLightsEnhancement/Components/TransitSignalPriorityDecisionTrace.cs) | Transient UI-facing debug and final-decision trace data. |
| Tram indexing | [`TramApproachIndex.cs`](../TrafficLightsEnhancement/Systems/TrafficLightSystems/Simulation/TramApproachIndex.cs) | Builds a per-tick lookup from tram track lane entity to tram curve position. |
| Request production | [`TransitSignalPriorityRuntime.cs`](../TrafficLightsEnhancement/Systems/TrafficLightSystems/Simulation/TransitSignalPriorityRuntime.cs) | Reads ECS/game state, detects approaching trams, builds or latches requests, and writes debug fields. |
| Normal signal application | [`PatchedTrafficLightSystem.cs`](../TrafficLightsEnhancement/Systems/TrafficLightSystems/Simulation/PatchedTrafficLightSystem.cs) | Resolves active TSP requests and applies them to normal traffic-light signal group selection. |
| Custom phase application | [`CustomStateMachine.cs`](../TrafficLightsEnhancement/Systems/TrafficLightSystems/Simulation/CustomStateMachine.cs) | Applies TSP hold/override policy to custom phase state machines. |
| UI and diagnostics | [`UISystem.UIBIndings.cs`](../TrafficLightsEnhancement/Systems/UI/UISystem.UIBIndings.cs), [`content.tsx`](../TrafficLightsEnhancement/UI/src/mods/components/main-panel/content.tsx) | Exposes selected-intersection TSP state, summary rows, recent events, and optional JSONL trace output. |

## Pure Logic Layer

The pure logic project lives in [`TrafficLightsEnhancement.Logic/Tsp`](../TrafficLightsEnhancement.Logic/Tsp). It targets `netstandard2.0` and has no Unity dependencies, so changes here should usually be covered by xUnit tests in [`TrafficLightsEnhancement.Tests/Tsp`](../TrafficLightsEnhancement.Tests/Tsp).

Key files:

- [`TspRequestInputs.cs`](../TrafficLightsEnhancement.Logic/Tsp/TspRequestInputs.cs) defines shared value types: `TspSource`, `PhaseScore`, `TspRequest`, and `TspDecision`.
- [`TransitSignalPrioritySettings.cs`](../TrafficLightsEnhancement.Logic/Tsp/TransitSignalPrioritySettings.cs) defines pure settings defaults and normalization. Current defaults are tram-only: track requests allowed, public-car requests reserved but disabled, request horizon `10`, and max green extension `45`.
- [`TransitSignalPriorityRuntime.cs`](../TrafficLightsEnhancement.Logic/Tsp/TransitSignalPriorityRuntime.cs) converts normalized settings plus lane classification into a `TspRequest`. Today it only emits `TspSource.Track` requests.
- [`EarlyApproachDetection.cs`](../TrafficLightsEnhancement.Logic/Tsp/EarlyApproachDetection.cs) contains pure helper policy for approach-lane resolution, tram-sample probing, early-vs-petitioner selection, and connected-edge fallback diagnostics.
- [`TspDecisionEngine.cs`](../TrafficLightsEnhancement.Logic/Tsp/TspDecisionEngine.cs) combines and scores requests. `CombineRequests()` selects the strongest track request; `SelectNextPhase()` can extend a current track-serving phase or bias selection toward track-serving phases.
- [`TspOverrideEngine.cs`](../TrafficLightsEnhancement.Logic/Tsp/TspOverrideEngine.cs) applies a selected TSP request to a base phase or signal group choice. It owns `TspSelectionReason` and `TspOverrideSelection`.
- [`TspPreemptionPolicy.cs`](../TrafficLightsEnhancement.Logic/Tsp/TspPreemptionPolicy.cs) owns request latching, current-group hold, aggressive preemption, minimum-green override, and exclusive-pedestrian protection.
- [`TspStatusFormatter.cs`](../TrafficLightsEnhancement.Logic/Tsp/TspStatusFormatter.cs) converts request state into UI-facing status labels.

Important boundary conventions:

- Signal groups in game/ECS data are 1-based. Pure phase indexes are usually 0-based. `TspOverrideEngine.ApplySignalGroupOverride()` is the bridge between those conventions.
- `TspSource.PublicCar` and `m_AllowPublicCarRequests` are reserved for future bus/public-transport work. Current runtime behavior is effectively track-only.
- Request horizon value `120` is treated as a legacy default and normalized to `10`. Changing that behavior affects compatibility with previously saved TSP settings.

## Saved Settings And Runtime Components

TSP uses one saved component and several transient runtime components.

[`TrafficLightsEnhancement/Components/TransitSignalPrioritySettings.cs`](../TrafficLightsEnhancement/Components/TransitSignalPrioritySettings.cs) is the saved per-intersection component. It implements `ISerializable`, writes a versioned payload, normalizes loaded values, and converts to the pure logic settings type through `ToLogicSettings()`.

Saved fields:

- `m_Enabled`
- `m_AllowTrackRequests`
- `m_AllowPublicCarRequests`
- `m_RequestHorizonTicks`
- `m_MaxGreenExtensionTicks`

[`TransitSignalPriorityRequest.cs`](../TrafficLightsEnhancement/Components/TransitSignalPriorityRequest.cs) is runtime latch state. It stores the target signal group, source, strength, expiry timer, and whether current-phase extension is allowed. It is added, updated, or removed each simulation tick by `PatchedTrafficLightSystem.UpdateTrafficLightsJob`.

[`TransitSignalPriorityRuntimeDebugInfo.cs`](../TrafficLightsEnhancement/Components/TransitSignalPriorityRuntimeDebugInfo.cs) is transient diagnostics state. It records candidate type, probe results, selected lanes and lane owners, sibling samples, master-lane flags, tram approach index size, and connected-edge fallback details.

[`TransitSignalPriorityDecisionTrace.cs`](../TrafficLightsEnhancement/Components/TransitSignalPriorityDecisionTrace.cs) is transient final-decision trace state. It records the requested target group, base group, selected group, request source, and selection reason used by the UI diagnostics panel.

## End-To-End Runtime Flow

The normal runtime path starts in [`PatchedTrafficLightSystem.OnUpdate()`](../TrafficLightsEnhancement/Systems/TrafficLightSystems/Simulation/PatchedTrafficLightSystem.cs).

1. `OnUpdate()` checks whether any `TransitSignalPrioritySettings` component exists through `TspPolicy.ShouldBuildApproachIndex(...)`.
2. If needed, [`TramApproachIndex.Build(...)`](../TrafficLightsEnhancement/Systems/TrafficLightSystems/Simulation/TramApproachIndex.cs) scans rail transit vehicles and builds a `NativeParallelHashMap<Entity, float>` from tram track lane entity to curve position.
3. `UpdateTrafficLightsJob.Execute(...)` calls `TransitSignalPriorityRuntime.TryResolveActiveLocalRequest(...)` before selecting the next signal group.
4. The runtime reads and normalizes ECS settings, rejects unavailable or grouped-follower intersections, builds a fresh request if possible, or latches a still-valid existing request.
5. If a request is active, the job writes `TransitSignalPriorityRequest` and `TransitSignalPriorityRuntimeDebugInfo`. If no request is active, stale request/debug components are removed.
6. Normal or custom signal selection receives `hasTspRequest` plus the active `TransitSignalPriorityRequest`.
7. If TSP changes or extends the selected group, `TransitSignalPriorityDecisionTrace` is written for diagnostics. If no TSP decision was made, stale decision traces are removed.

## Request Production

The integration runtime lives in [`TrafficLightsEnhancement/Systems/TrafficLightSystems/Simulation/TransitSignalPriorityRuntime.cs`](../TrafficLightsEnhancement/Systems/TrafficLightSystems/Simulation/TransitSignalPriorityRuntime.cs). Its main entry point is `TryResolveActiveLocalRequest(...)`.

Fresh request detection:

- `TryBuildFreshRequest(...)` scans junction sublanes with `LaneSignal`.
- For each track lane, it resolves the approach lane using `ExtraLaneSignal.m_SourceSubLane` when available.
- Early approach candidates are built by `TryBuildEarlyApproachRequestForTrackLane(...)`.
- Petitioner candidates are built by `TryBuildPetitionerRequestForLane(...)`.
- `EarlyApproachDetection.PreferEarlyRequest(...)` prefers early approach candidates over petitioner candidates.

The tram approach index is intentionally narrow:

- It scans vehicles with `PublicTransport`, `TrainNavigation`, and `TrainCurrentLane`.
- It records front and rear track-lane samples for moving trams that are not boarding.
- It keeps the smallest curve position per lane, representing the earliest relevant tram sample on that lane.
- `Arriving` and `RequireStop` do not suppress indexing; stopped/boarding behavior is handled by movement and boarding checks.

The early approach detector checks several lane candidates:

- the signaled lane,
- the resolved approach lane,
- connected-edge fallback lanes,
- immediate upstream lanes,
- connected upstream lanes.

Current thresholds are implementation details in the runtime: approach-lane checks use `0.2f`, upstream checks use `0.9f`, and connected-edge fallback uses `0f`.

Request latching is pure policy:

- Fresh eligible track requests receive the effective request horizon.
- Existing requests decrement while source, target, strength, and expiry remain valid.
- Expired or invalid requests are removed from ECS state.

## Applying Requests To Signals

Normal signal selection is handled in [`PatchedTrafficLightSystem.cs`](../TrafficLightsEnhancement/Systems/TrafficLightSystems/Simulation/PatchedTrafficLightSystem.cs).

- `UpdateTrafficLightState(...)` receives settings and active request state.
- `GetNextSignalGroup(...)` computes the base group through `GetNextSignalGroupWithoutTsp(...)`.
- `TspPreemptionPolicy.ShouldAggressivelyPreemptToConflictingGroup(...)` can shorten minimum green for a conflicting track request.
- `TspOverrideEngine.ApplySignalGroupOverride(...)` changes the selected group or reports current-group extension.
- `TryApplyTspCurrentGroupHold(...)` and `TspPreemptionPolicy.ShouldHoldCurrentGroup(...)` hold a compatible current group while the request remains valid and the max extension limit has not been reached.

Custom phase selection is handled in [`CustomStateMachine.cs`](../TrafficLightsEnhancement/Systems/TrafficLightSystems/Simulation/CustomStateMachine.cs).

- `UpdateTrafficLightState(...)` receives the same active request from the patched system.
- During an ongoing custom phase, `TransitSignalPriorityRuntime.ShouldHoldCurrentGroup(...)` can extend the current group.
- `GetNextSignalGroup(...)` computes the base fixed/dynamic custom phase.
- `ApplyTspOverride(...)` calls `TspOverrideEngine.ApplyRequestOverride(...)` to select a target custom group when appropriate.

Exclusive pedestrian protection is shared policy. `TspPreemptionPolicy.ShouldProtectActivePedestrianPhase(...)` returns true only when exclusive pedestrian mode is enabled, the current phase is ongoing, the current group is in range, and `CustomTrafficLights.m_PedestrianPhaseGroupMask` contains the current group. When protection is active, TSP can hold the current group if it already serves the request, but it should not preempt away from the active pedestrian group.

## UI And Selected-Intersection Diagnostics

The UI binding layer is [`TrafficLightsEnhancement/Systems/UI/UISystem.UIBIndings.cs`](../TrafficLightsEnhancement/Systems/UI/UISystem.UIBIndings.cs).

Selected panel flow:

1. `GetMainPanel()` reads the selected entity's `TransitSignalPrioritySettings`, defaulting to `CreateDefault()` if the component is absent.
2. It attaches `mainData.tramSignalPriority` with visibility, enabled/editable state, status label, and optional diagnostics.
3. TypeScript consumes the binding through `useMainPanel()` in [`UI/src/mods/components/main-panel/index.tsx`](../TrafficLightsEnhancement/UI/src/mods/components/main-panel/index.tsx).
4. [`content.tsx`](../TrafficLightsEnhancement/UI/src/mods/components/main-panel/content.tsx) renders the TSP toggle, follower status, summary, recent events, and diagnostic rows.

The toggle path is also in `UISystem.UIBIndings.cs`:

- The UI calls `toggleTramSignalPriority(...)`.
- The C# trigger `ToggleTramSignalPriority(...)` creates or updates `TransitSignalPrioritySettings` when enabled.
- Disabling TSP removes `TransitSignalPrioritySettings` from the selected entity and marks it updated.
- Follower intersections cannot toggle TSP; leader intersections remain editable.

Diagnostics are off by default. The mod option is `Settings.m_ShowTramSignalPriorityDiagnostics`; `GetMainPanel()` only builds diagnostics when that option is true. `UISystem.SimulationUpdate()` also only auto-refreshes the selected panel for diagnostics when the same option is enabled.

`GetTramSignalPriorityDiagnostics(...)` reads:

- `TrafficLights` for current signal state,
- `TransitSignalPriorityRuntimeDebugInfo` for probe and request-candidate details,
- `TransitSignalPriorityDecisionTrace` for the final selected/base/requested groups and reason.

It returns:

- `summary`: compact selected-intersection state,
- `events`: recent changed signatures for the selected entity,
- `rows`: detailed diagnostic key/value rows.

## JSONL Trace Output

When diagnostics are enabled and the selected panel asks for diagnostics, `UISystem.UIBIndings.cs` can write a trace file:

- file name: `C2VM.TrafficLightsEnhancement.TspDiagnostics.jsonl`,
- location: `Application.persistentDataPath`,
- writer: `GetTspDiagnosticsEvents(...)` and related helpers,
- rotation threshold: 5 MB,
- rotated file retention: newest 3 rotated files,
- dedupe: `TspDiagnosticsHistory.LastSignature` suppresses repeated identical selected-entity summaries.

The trace is a debugging aid, not gameplay state. It should remain safe to disable, delete, or rotate without affecting TSP behavior.

## Test Coverage

TSP pure logic is covered by xUnit tests in [`TrafficLightsEnhancement.Tests/Tsp`](../TrafficLightsEnhancement.Tests/Tsp):

- [`TspPolicyTests.cs`](../TrafficLightsEnhancement.Tests/Tsp/TspPolicyTests.cs) covers availability, grouped-intersection policy, default settings, persisted-value detection, normalization/clamping, approach-index policy, and pedestrian mask bounds.
- [`TspEarlyDetectionTests.cs`](../TrafficLightsEnhancement.Tests/Tsp/TspEarlyDetectionTests.cs) covers lane resolution, indexed probe precedence, movement suppression, early-over-petitioner selection, connected-edge fallback helpers, and upstream/path-node helpers.
- [`TspDecisionEngineTests.cs`](../TrafficLightsEnhancement.Tests/Tsp/TspDecisionEngineTests.cs) covers track-only requests, public-car non-participation, null/empty handling, extension rules, override behavior, latching/expiry, hold policy, aggressive preemption, and active pedestrian protection.
- [`TspStatusFormatterTests.cs`](../TrafficLightsEnhancement.Tests/Tsp/TspStatusFormatterTests.cs) covers status label formatting.

UI-facing behavior also has Node tests in [`TrafficLightsEnhancement/UI/tests/tram-signal-priority-panel.test.mjs`](../TrafficLightsEnhancement/UI/tests/tram-signal-priority-panel.test.mjs), including panel state, diagnostics gating, trace writing, toggling, cleanup, and serialization expectations.

## Caveats For Future Work

- Public-car/bus priority is deliberately not implemented yet. Existing `PublicCar` fields are reserved inputs, not active behavior.
- `TramApproachIndex` is built whenever any TSP settings component exists, not only when enabled intersections exist. That is simple and safe, but broader than necessary.
- Grouped intersections currently reject non-leader runtime TSP requests. Group-wide TSP would need explicit leader/follower semantics.
- Runtime diagnostics are transient ECS data, but UI code depends on their field meanings. Treat renames/removals as UI-impacting.
- Connected-edge fallback is topology-sensitive and diagnostics-heavy. If lane-resolution rules change, update both runtime diagnostics and this document.
- The selected-intersection JSONL trace is synchronous UI-triggered file I/O. Keep it opt-in unless it is redesigned.
