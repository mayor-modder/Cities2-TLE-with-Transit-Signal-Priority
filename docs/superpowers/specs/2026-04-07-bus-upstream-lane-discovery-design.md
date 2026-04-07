# Bus Upstream Lane Discovery Diagnostic

**Date:** 2026-04-07
**Branch:** `codex/bus-tsp-runtime-diagnostics`
**Status:** Diagnostic instrumentation only (no behavioral change)

## Problem

Bus TSP early detection fails because `approachLaneEntity` collapses to `signaledLaneEntity` (the junction-owned lane). The bus is on an upstream road lane, but the system scans the junction lane and finds zero lane objects.

Tram TSP works because it has multi-tier upstream lane resolution (`TryResolveImmediateUpstreamTramLane`, `TryResolveConnectedUpstreamTramLane`). Buses have no equivalent.

Before building a bus upstream lane resolver, we need to know which lane topology applies:
- **Sibling sublanes:** The upstream bus lane is a sibling sublane of the same owner edge (like trams).
- **Connected edges:** The upstream bus lane is on a connected edge of the junction node.

## Solution

Add a `TryDiscoverUpstreamBusLane` diagnostic method that tries both strategies, records which one found a `PublicOnly` car lane, and surfaces the result in the debug panel. No behavioral change.

## Changes

### 1. New enum: `BusUpstreamDiscovery`

Location: `EarlyApproachDetection.cs` (alongside existing bus probe enums).

```
None = 0                 // resolver didn't run or no PublicCar lane in loop
NoOwner = 1              // approach lane has no Owner component
NoLaneData = 2           // approach lane has no Lane component
SiblingMatch = 3         // found PublicOnly car lane via sibling sublanes
ConnectedEdgeMatch = 4   // found PublicOnly car lane via connected edges
BothMatch = 5            // found via both strategies
NoCandidates = 6         // searched both, found nothing
```

### 2. New fields on `BusProbeDebugInfo`

Location: `TransitSignalPriorityRuntime.cs`, the local struct.

```csharp
public BusUpstreamDiscovery UpstreamDiscovery;
public Entity UpstreamSiblingEntity;        // candidate from sibling strategy
public Entity UpstreamConnectedEdgeEntity;  // candidate from connected edge strategy
public byte UpstreamSiblingSubLaneCount;    // sibling sublanes examined
public byte UpstreamConnectedEdgeCount;     // connected edges examined
public byte UpstreamBusLaneCandidateCount;  // total PublicOnly car lanes found across both
```

### 3. New fields on `TransitSignalPriorityRuntimeDebugInfo`

Location: `TransitSignalPriorityRuntimeDebugInfo.cs` (the ECS component that reaches the UI).

```csharp
// Encoded BusUpstreamDiscovery.
public byte m_BusUpstreamDiscovery;
public Entity m_BusUpstreamSiblingEntity;
public Entity m_BusUpstreamConnectedEdgeEntity;
public byte m_BusUpstreamSiblingSubLaneCount;
public byte m_BusUpstreamConnectedEdgeCount;
public byte m_BusUpstreamBusLaneCandidateCount;
```

### 4. New method: `TryDiscoverUpstreamBusLane`

Location: `TransitSignalPriorityRuntime.cs`, near `TryResolveImmediateUpstreamTramLane`.

Pseudocode:

```
TryDiscoverUpstreamBusLane(job, approachLaneEntity, ref busDebugInfo):
    if no Owner on approachLaneEntity:
        busDebugInfo.UpstreamDiscovery = NoOwner
        return

    if no Lane data on approachLaneEntity:
        busDebugInfo.UpstreamDiscovery = NoLaneData
        return

    ownerEntity = owner.m_Owner
    approachStartNode = approachLane.m_StartNode

    // Strategy 1: Sibling sublanes of the same owner
    siblingCandidate = Entity.Null
    if TryGetBuffer(ownerEntity, subLanes):
        busDebugInfo.UpstreamSiblingSubLaneCount = subLanes.Length
        for each sublane in subLanes:
            if sublane == approachLaneEntity: skip
            if not IsPublicOnlyCarLane(sublane): skip
            if not has Lane data: skip
            if candidateLane.m_EndNode == approachStartNode:
                siblingCandidate = sublane
                break
    busDebugInfo.UpstreamSiblingEntity = siblingCandidate

    // Strategy 2: Connected edges of the junction
    connectedEdgeCandidate = Entity.Null
    junctionEntity = ownerEntity  // or owner's owner if approach lane is edge-owned
    // Need to walk up: approach lane -> owner edge -> owner node (junction)
    // If ownerEntity is an edge, its owner might be the junction node
    // Try connected edges on the junction node
    if TryGetBuffer(junctionEntity, connectedEdges)
       OR (has Owner on ownerEntity -> junctionEntity2, TryGetBuffer(junctionEntity2, connectedEdges)):
        busDebugInfo.UpstreamConnectedEdgeCount = connectedEdges.Length
        for each connectedEdge:
            edgeEntity = connectedEdge.m_Edge
            if edgeEntity == ownerEntity: skip
            if not TryGetBuffer(edgeEntity, edgeSubLanes): skip
            for each edgeSublane:
                if not IsPublicOnlyCarLane(edgeSublane): skip
                busDebugInfo.UpstreamBusLaneCandidateCount++
                if not has Lane data: skip
                if candidateLane.m_EndNode connects to approachStartNode:
                    connectedEdgeCandidate = edgeSublane
                    break on first match
    busDebugInfo.UpstreamConnectedEdgeEntity = connectedEdgeCandidate

    // Classify result
    if siblingCandidate != Null && connectedEdgeCandidate != Null:
        busDebugInfo.UpstreamDiscovery = BothMatch
    else if siblingCandidate != Null:
        busDebugInfo.UpstreamDiscovery = SiblingMatch
    else if connectedEdgeCandidate != Null:
        busDebugInfo.UpstreamDiscovery = ConnectedEdgeMatch
    else:
        busDebugInfo.UpstreamDiscovery = NoCandidates
```

Note on node connectivity for connected edges: The tram connected edge resolver uses `m_EndNode.GetOwnerIndex()` comparison rather than direct `m_EndNode.Equals()`. The bus version should try both — direct equality first, owner-index equality as fallback — and record which one matched. This avoids assumptions about how bus lane nodes are indexed.

### 5. Call site

Location: `TransitSignalPriorityRuntime.cs`, in the `else if (isPublicCarLane)` block (around line 294).

Called unconditionally when processing a bus lane sublane, before the existing early probe call. The discovery result flows into `busDebugInfo` and from there into the ECS debug component via the existing `BuildRuntimeDebugInfo` path.

### 6. Panel rendering

Location: `UISystem.UIBIndings.cs`.

Add to the `bus[...]` line:

```
upstream=<discovery-enum>
```

Add a new line after `entities[...]`:

```
upstream[sibling=<entity>, connected=<entity>, siblingLanes=<count>, connectedEdges=<count>, busCandidates=<count>]
```

Add `FormatBusUpstreamDiscovery(byte)` formatter and include `m_BusUpstreamDiscovery` in `HasVisibleBusDiagnostics`.

### 7. Tests

Add test coverage for `BusUpstreamDiscovery` enum values in `TspEarlyDetectionTests.cs` — verify the enum is defined correctly and the formatter covers all variants. The discovery method itself is runtime-only (needs ECS job context), so it's tested via in-game observation.

## What this does NOT do

- Does not change request selection or signal behavior
- Does not use the discovered upstream lane for bus detection (that's the next step)
- Does not change tram behavior

## Expected outcome

After deployment, the panel shows something like:

```
bus[early=none, petitioner=missing, upstream=connected-edge-match, ...]
upstream[sibling=null, connected=12345:1, siblingLanes=4, connectedEdges=3, busCandidates=1]
```

This tells us exactly which strategy to build the real resolver on.

## Files touched

1. `TrafficLightsEnhancement.Logic/Tsp/EarlyApproachDetection.cs` — new enum
2. `TrafficLightsEnhancement/Systems/TrafficLightSystems/Simulation/TransitSignalPriorityRuntime.cs` — new method, expanded struct, call site
3. `TrafficLightsEnhancement/Components/TransitSignalPriorityRuntimeDebugInfo.cs` — new fields
4. `TrafficLightsEnhancement/Systems/UI/UISystem.UIBIndings.cs` — new formatter, panel line
5. `TrafficLightsEnhancement.Tests/Tsp/TspEarlyDetectionTests.cs` — enum coverage
