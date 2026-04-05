# TSP No-Grace Early Approach Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the local TSP release grace and add earlier tram-approach detection so tram requests arrive before the stop-line petitioner path.

**Architecture:** Keep the request lifecycle split between pure TSP policy in `TrafficLightsEnhancement.Logic` and ECS-heavy vehicle detection in the simulation runtime. Reuse the existing request object shape, add a small pure early-detection helper, and update the runtime to prefer an early tram request before falling back to the petitioner path.

**Tech Stack:** C#, xUnit, Unity ECS component lookups, Cities II simulation/runtime data

---

### Task 1: Remove the Local Release Grace

**Files:**
- Modify: `TrafficLightsEnhancement.Logic/Tsp/TspPreemptionPolicy.cs`
- Modify: `TrafficLightsEnhancement.Tests/Tsp/TspDecisionEngineTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
[Fact]
public void Stale_signal_request_counts_down_normally_when_target_group_is_current_group()
{
    bool active = TspPreemptionPolicy.TryRefreshOrLatchRequest(
        freshRequest: null,
        existingRequest: new TspSignalRequest(targetSignalGroup: 2, TspSource.Track, strength: 1f, expiryTimer: 120, extendCurrentPhase: false),
        requestHorizonTicks: 120,
        currentSignalGroup: 2,
        out var request);

    Assert.True(active);
    Assert.Equal(119u, request.ExpiryTimer);
    Assert.False(request.ExtendCurrentPhase);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test TrafficLightsEnhancement.Tests\\TrafficLightsEnhancement.Tests.csproj --filter Stale_signal_request_counts_down_normally_when_target_group_is_current_group`

Expected: FAIL because the current policy still collapses to `6`.

- [ ] **Step 3: Write the minimal implementation**

```csharp
if (existingRequest.HasValue && existingRequest.Value.ExpiryTimer > 1)
{
    TspSignalRequest existing = existingRequest.Value;
    uint nextExpiry = existing.ExpiryTimer - 1;

    request = new TspSignalRequest(
        existing.TargetSignalGroup,
        existing.Source,
        existing.Strength,
        nextExpiry,
        existing.ExtendCurrentPhase);
    return true;
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test TrafficLightsEnhancement.Tests\\TrafficLightsEnhancement.Tests.csproj --filter TspDecisionEngineTests`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add TrafficLightsEnhancement.Logic/Tsp/TspPreemptionPolicy.cs TrafficLightsEnhancement.Tests/Tsp/TspDecisionEngineTests.cs
git commit -m "fix: remove local tsp release grace"
```

### Task 2: Add Early Tram Approach Detection

**Files:**
- Create: `TrafficLightsEnhancement.Logic/Tsp/EarlyApproachDetection.cs`
- Create: `TrafficLightsEnhancement.Tests/Tsp/TspEarlyDetectionTests.cs`
- Modify: `TrafficLightsEnhancement/Systems/TrafficLightSystems/Simulation/ExtraTypeHandle.cs`
- Modify: `TrafficLightsEnhancement/Systems/TrafficLightSystems/Simulation/TransitSignalPriorityRuntime.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
[Fact]
public void Tram_detection_matches_immediate_upstream_lane()
{
    bool matched = EarlyApproachDetection.IsEligibleTramApproachLane(
        currentLane: 10,
        approachLane: 20,
        upstreamLane: 10,
        nullLane: 0);

    Assert.True(matched);
}

[Fact]
public void Scan_wide_selection_falls_back_to_petitioner_when_early_request_is_absent()
{
    TspRequest petitioner = new(TspSource.Track, strength: 0.8f, extensionEligible: true);
    TransitApproachScanState scanState = default;

    scanState = EarlyApproachDetection.RecordLaneRequests(
        scanState,
        earlyRequest: null,
        petitionerRequest: petitioner);

    TspRequest? selected = EarlyApproachDetection.PreferEarlyRequest(
        scanState.EarlyRequest,
        scanState.PetitionerRequest);

    Assert.True(selected.HasValue);
    Assert.Equal(petitioner.Source, selected.Value.Source);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test TrafficLightsEnhancement.Tests\\TrafficLightsEnhancement.Tests.csproj --filter TspEarlyDetectionTests`

Expected: FAIL because `EarlyApproachDetection` and its types do not exist yet.

- [ ] **Step 3: Add the pure early-detection helper**

```csharp
public static bool IsEligibleTramApproachLane<TEntity>(
    TEntity currentLane,
    TEntity approachLane,
    TEntity upstreamLane,
    TEntity nullLane)
    where TEntity : struct, IEquatable<TEntity>
{
    return currentLane.Equals(approachLane)
        || (!upstreamLane.Equals(nullLane) && currentLane.Equals(upstreamLane));
}
```

- [ ] **Step 4: Extend the runtime lookups and request builder**

```csharp
public ComponentLookup<PrefabRef> m_PrefabRef;
public ComponentLookup<TrackLaneData> m_TrackLaneData;
public ComponentLookup<PublicTransport> m_PublicTransport;
public ComponentLookup<TrainNavigation> m_TrainNavigation;
public ComponentLookup<TrainCurrentLane> m_TrainCurrentLane;
public ComponentLookup<CarCurrentLane> m_CarCurrentLane;
```

```csharp
if (allowEarlyApproachDetection
    && TryBuildEarlyApproachRequestForLane(
        job,
        approachLaneEntity,
        isTramTrackLane,
        isPublicCarLane,
        logicRequest,
        out var detectedEarlyRequest))
{
    earlyRequest = detectedEarlyRequest;
}
```

- [ ] **Step 5: Run focused tests**

Run: `dotnet test TrafficLightsEnhancement.Tests\\TrafficLightsEnhancement.Tests.csproj --filter TspEarlyDetectionTests`

Expected: PASS

- [ ] **Step 6: Run broader regression checks**

Run: `dotnet test TrafficLightsEnhancement.Tests\\TrafficLightsEnhancement.Tests.csproj --filter "TspDecisionEngineTests|TspEarlyDetectionTests|TspStatusFormatterTests|MainPanelRefreshPolicyTests"`

Expected: PASS

- [ ] **Step 7: Run compile verification**

Run: `dotnet build TrafficLightsEnhancement\\TrafficLightsEnhancement.csproj -c Debug -p:DisablePostProcessors=true`

Expected: SUCCESS

- [ ] **Step 8: Commit**

```bash
git add TrafficLightsEnhancement.Logic/Tsp/EarlyApproachDetection.cs TrafficLightsEnhancement.Tests/Tsp/TspEarlyDetectionTests.cs TrafficLightsEnhancement/Systems/TrafficLightSystems/Simulation/ExtraTypeHandle.cs TrafficLightsEnhancement/Systems/TrafficLightSystems/Simulation/TransitSignalPriorityRuntime.cs
git commit -m "feat: detect tram tsp requests earlier"
```
