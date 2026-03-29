# TSP Early Approach Detection Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Detect eligible moving trams and buses slightly before the current lane-signal petitioner point so Transit Signal Priority responds earlier, while also shortening the selected-intersection panel label from `Transit Signal Priority Status` to `Status` and renaming the track toggle to `Allow Tram Requests`.

**Architecture:** Keep the existing TSP request lifecycle, override logic, and grouped-intersection guardrails intact. Add a small early-approach detection helper in the simulation runtime that runs before the current petitioner-based request builder, cover the new behavior with focused tests, and update the existing UI copy keys without changing the surrounding panel structure.

**Tech Stack:** C#/.NET 8 tests, Unity ECS runtime code for Cities: Skylines II, TypeScript/React UI bindings, webpack UI build, xUnit.

---

## File Map

- `TrafficLightsEnhancement.Tests/Tsp/TspEarlyDetectionTests.cs`
  - New focused tests for early moving-vehicle detection helpers and petitioner fallback behavior.
- `TrafficLightsEnhancement.Logic/Tsp/TransitSignalPriorityRuntime.cs`
  - Keep pure policy limited to lane-type eligibility; no early ECS detection logic belongs here.
- `TrafficLightsEnhancement/Systems/TrafficLightSystems/Simulation/TransitSignalPriorityRuntime.cs`
  - Add early-approach helper(s), wire them into fresh-request building, and preserve petitioner fallback.
- `TrafficLightsEnhancement/Systems/TrafficLightSystems/Simulation/PatchedTrafficLightSystem.cs`
  - Only touch if the runtime helper needs extra component access or call-site wiring.
- `TrafficLightsEnhancement/Systems/TrafficLightSystems/Simulation/ExtraTypeHandle.cs`
  - Add any component lookups required for early moving-vehicle detection.
- `TrafficLightsEnhancement/Systems/UI/UISystem.cs`
  - No behavioral changes expected beyond using the existing status payload.
- `TrafficLightsEnhancement/UI/src/mods/localisations/en-US.ts`
  - Rename `TspStatusLabel` value to `Status` and `AllowTrackTransitRequests` to `Allow Tram Requests`.

### Task 1: Lock In Early-Detection Tests

**Files:**
- Create: `TrafficLightsEnhancement.Tests/Tsp/TspEarlyDetectionTests.cs`
- Modify: `TrafficLightsEnhancement.Tests/TrafficLightsEnhancement.Tests.csproj`
- Test: `TrafficLightsEnhancement.Tests/Tsp/TspEarlyDetectionTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using TrafficLightsEnhancement.Logic.Tsp;
using Xunit;

namespace TrafficLightsEnhancement.Tests.Tsp;

public sealed class TspEarlyDetectionTests
{
    [Fact]
    public void Early_detection_accepts_moving_track_vehicle()
    {
        bool detected = EarlyApproachDetection.ShouldTrigger(
            isEligibleLane: true,
            isVehicleMoving: true);

        Assert.True(detected);
    }

    [Fact]
    public void Early_detection_rejects_stopped_vehicle()
    {
        bool detected = EarlyApproachDetection.ShouldTrigger(
            isEligibleLane: true,
            isVehicleMoving: false);

        Assert.False(detected);
    }

    [Fact]
    public void Early_detection_rejects_ineligible_lane()
    {
        bool detected = EarlyApproachDetection.ShouldTrigger(
            isEligibleLane: false,
            isVehicleMoving: true);

        Assert.False(detected);
    }

    [Fact]
    public void Existing_petitioner_path_remains_available_when_early_detection_fails()
    {
        var request = EarlyApproachDetection.SelectFreshRequest(
            earlyRequest: null,
            petitionerRequest: new TspRequest(TspSource.PublicCar, 1f, extensionEligible: false));

        Assert.NotNull(request);
        Assert.Equal(TspSource.PublicCar, request.Value.Source);
    }
}
```

- [ ] **Step 2: Run the focused tests to verify they fail**

Run: `dotnet test TrafficLightsEnhancement.Tests/TrafficLightsEnhancement.Tests.csproj --filter TspEarlyDetectionTests -v minimal`

Expected: FAIL with missing `EarlyApproachDetection` type or members.

- [ ] **Step 3: Add the smallest shared helper surface needed by the tests**

```csharp
namespace TrafficLightsEnhancement.Logic.Tsp;

public static class EarlyApproachDetection
{
    public static bool ShouldTrigger(bool isEligibleLane, bool isVehicleMoving)
    {
        return isEligibleLane && isVehicleMoving;
    }

    public static TspRequest? SelectFreshRequest(TspRequest? earlyRequest, TspRequest? petitionerRequest)
    {
        return earlyRequest ?? petitionerRequest;
    }
}
```

- [ ] **Step 4: Run the focused tests to verify they pass**

Run: `dotnet test TrafficLightsEnhancement.Tests/TrafficLightsEnhancement.Tests.csproj --filter TspEarlyDetectionTests -v minimal`

Expected: PASS with `4` tests passed.

- [ ] **Step 5: Commit the test scaffold**

```bash
git add TrafficLightsEnhancement.Tests/Tsp/TspEarlyDetectionTests.cs TrafficLightsEnhancement.Logic/Tsp/TransitSignalPriorityRuntime.cs
git commit -m "test: add TSP early detection coverage"
```

### Task 2: Implement Early Moving-Vehicle Detection in Runtime

**Files:**
- Modify: `TrafficLightsEnhancement/Systems/TrafficLightSystems/Simulation/TransitSignalPriorityRuntime.cs`
- Modify: `TrafficLightsEnhancement/Systems/TrafficLightSystems/Simulation/ExtraTypeHandle.cs`
- Modify: `TrafficLightsEnhancement/Systems/TrafficLightSystems/Simulation/PatchedTrafficLightSystem.cs`
- Test: `TrafficLightsEnhancement.Tests/Tsp/TspEarlyDetectionTests.cs`

- [ ] **Step 1: Add a failing behavior test for source ordering**

```csharp
[Fact]
public void SelectFreshRequest_prefers_early_request_over_petitioner_request()
{
    var earlyRequest = new TspRequest(TspSource.Track, 1f, extensionEligible: true);
    var petitionerRequest = new TspRequest(TspSource.PublicCar, 1f, extensionEligible: false);

    var request = EarlyApproachDetection.SelectFreshRequest(earlyRequest, petitionerRequest);

    Assert.NotNull(request);
    Assert.Equal(TspSource.Track, request.Value.Source);
}
```

- [ ] **Step 2: Run the focused tests to verify the new test fails if the ordering is wrong**

Run: `dotnet test TrafficLightsEnhancement.Tests/TrafficLightsEnhancement.Tests.csproj --filter TspEarlyDetectionTests -v minimal`

Expected: FAIL if `SelectFreshRequest` does not prefer `earlyRequest`.

- [ ] **Step 3: Implement runtime early detection before petitioner fallback**

```csharp
private static bool TryBuildFreshRequest(
    PatchedTrafficLightSystem.UpdateTrafficLightsJob job,
    DynamicBuffer<SubLane> subLanes,
    TrafficLights trafficLights,
    Components.TransitSignalPrioritySettings settings,
    out TransitSignalPriorityRequest request)
{
    request = default;

    TransitSignalPriorityRequest earlyRequest = default;
    if (TryBuildEarlyApproachRequest(job, subLanes, trafficLights, settings, out earlyRequest))
    {
        request = earlyRequest;
        return true;
    }

    return TryBuildPetitionerRequest(job, subLanes, trafficLights, settings, out request);
}
```

```csharp
private static bool TryBuildEarlyApproachRequest(
    PatchedTrafficLightSystem.UpdateTrafficLightsJob job,
    DynamicBuffer<SubLane> subLanes,
    TrafficLights trafficLights,
    Components.TransitSignalPrioritySettings settings,
    out TransitSignalPriorityRequest request)
{
    request = default;

    foreach (var subLane in subLanes)
    {
        Entity subLaneEntity = subLane.m_SubLane;
        bool isTrackLane = job.m_ExtraTypeHandle.m_TrackLane.HasComponent(subLaneEntity);
        bool isPublicCarLane = IsPublicOnlyCarLane(job, subLaneEntity);

        if (!global::TrafficLightsEnhancement.Logic.Tsp.EarlyApproachDetection.ShouldTrigger(
                isEligibleLane: isTrackLane || isPublicCarLane,
                isVehicleMoving: IsTransitVehicleMoving(job, subLaneEntity)))
        {
            continue;
        }

        if (!TryBuildLaneRequest(job, subLaneEntity, trafficLights, settings, isTrackLane, isPublicCarLane, out request))
        {
            continue;
        }

        return true;
    }

    return false;
}
```

```csharp
private static bool TryBuildPetitionerRequest(
    PatchedTrafficLightSystem.UpdateTrafficLightsJob job,
    DynamicBuffer<SubLane> subLanes,
    TrafficLights trafficLights,
    Components.TransitSignalPrioritySettings settings,
    out TransitSignalPriorityRequest request)
{
    request = default;

    foreach (var subLane in subLanes)
    {
        Entity subLaneEntity = subLane.m_SubLane;
        if (!job.m_LaneSignalData.TryGetComponent(subLaneEntity, out var laneSignal) || laneSignal.m_Petitioner == Entity.Null)
        {
            continue;
        }

        bool isTrackLane = job.m_ExtraTypeHandle.m_TrackLane.HasComponent(subLaneEntity);
        bool isPublicCarLane = IsPublicOnlyCarLane(job, subLaneEntity);
        if (TryBuildLaneRequest(job, subLaneEntity, trafficLights, settings, isTrackLane, isPublicCarLane, out request))
        {
            return true;
        }
    }

    return false;
}
```

- [ ] **Step 4: Run tests and targeted build verification**

Run: `dotnet test TrafficLightsEnhancement.Tests/TrafficLightsEnhancement.Tests.csproj --filter TspEarlyDetectionTests -v minimal`

Expected: PASS with the ordering and stopped-vehicle tests green.

Run: `dotnet test TrafficLightsEnhancement.Tests/TrafficLightsEnhancement.Tests.csproj -v minimal`

Expected: PASS with the full test suite green.

- [ ] **Step 5: Commit the runtime detection change**

```bash
git add TrafficLightsEnhancement/Systems/TrafficLightSystems/Simulation/TransitSignalPriorityRuntime.cs TrafficLightsEnhancement/Systems/TrafficLightSystems/Simulation/ExtraTypeHandle.cs TrafficLightsEnhancement/Systems/TrafficLightSystems/Simulation/PatchedTrafficLightSystem.cs TrafficLightsEnhancement.Tests/Tsp/TspEarlyDetectionTests.cs
git commit -m "feat: detect TSP requests earlier for moving transit"
```

### Task 3: Update TSP Copy in the Selected-Intersection Panel

**Files:**
- Modify: `TrafficLightsEnhancement/UI/src/mods/localisations/en-US.ts`
- Test: `TrafficLightsEnhancement/UI/src/mods/localisations/en-US.ts`

- [ ] **Step 1: Change the English copy to match the approved wording**

```ts
export default {
  TransitSignalPriority: "Transit Signal Priority",
  TspStatusLabel: "Status",
  AllowTrackTransitRequests: "Allow Tram Requests",
};
```

- [ ] **Step 2: Run the UI build to verify localization changes compile cleanly**

Run: `npm --prefix TrafficLightsEnhancement/UI run build`

Expected: PASS with the existing Sass deprecation warnings only.

- [ ] **Step 3: Commit the copy cleanup**

```bash
git add TrafficLightsEnhancement/UI/src/mods/localisations/en-US.ts
git commit -m "chore: simplify TSP panel copy"
```

### Task 4: End-to-End Verification and Deploy

**Files:**
- Verify only: `TrafficLightsEnhancement.Tests/TrafficLightsEnhancement.Tests.csproj`
- Verify only: `TrafficLightsEnhancement/TrafficLightsEnhancement.csproj`
- Verify only: `TrafficLightsEnhancement/UI`

- [ ] **Step 1: Run the complete automated verification suite**

Run: `dotnet test TrafficLightsEnhancement.Tests/TrafficLightsEnhancement.Tests.csproj`

Expected: PASS with all TSP tests, including the new early-detection coverage.

Run: `npm --prefix TrafficLightsEnhancement/UI run build`

Expected: PASS with only the existing Sass deprecation warnings.

Run: `dotnet build TrafficLightsEnhancement/TrafficLightsEnhancement.csproj`

Expected: PASS and deploy the mod when Cities: Skylines II is closed.

- [ ] **Step 2: Manually sanity-check the intended in-game behaviors**

Run this checklist in-game:
- pick a standalone tram or bus intersection with TSP enabled
- verify the status row reads `Status`
- verify the request filter reads `Allow Tram Requests`
- verify TSP becomes visible earlier than before, before the transit vehicle is already at the stop line
- verify a stopped vehicle at a near-side stop does not appear to trigger early TSP until it leaves the stop

- [ ] **Step 3: Commit any final verification-only adjustments if needed**

```bash
git status --short
git add <only-files-you-actually-adjusted>
git commit -m "fix: polish TSP early detection" 
```

If no follow-up changes are needed after verification, skip the commit and leave the branch clean.

## Self-Review

- Spec coverage:
  - earlier transit detection: Task 2
  - preserve petitioner fallback and lifecycle: Task 2
  - stopped vehicles must not trigger: Tasks 1 and 2
  - grouped intersections remain unchanged: Task 2 plus full-suite verification
  - status label shortened to `Status`: Task 3
  - `Allow Tram Requests` wording: Task 3
- Placeholder scan:
  - no `TODO`, `TBD`, or unresolved placeholders remain
- Type consistency:
  - helper names are consistent across the test and runtime tasks: `ShouldTrigger`, `SelectFreshRequest`, `TryBuildEarlyApproachRequest`, `TryBuildPetitionerRequest`

