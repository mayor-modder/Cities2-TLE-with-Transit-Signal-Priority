# README Fork Rewrite Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Rewrite the root README so it describes `Cities2-TLE-TSP` as a fork-first project with exact, verified documentation for the TSP additions.

**Architecture:** Replace the inherited upstream project-page structure with a fork-first narrative. Use current code, UI bindings, tests, and recent fork commits as the source of truth for concrete claims, and add explicit "known limits / uncertainty" language wherever behavior is not fully validated.

**Tech Stack:** Markdown, git history inspection, C# ECS mod code, TypeScript/React UI bindings, local shell verification.

---

### Task 1: Gather verified README facts

**Files:**
- Modify: `README.md`
- Reference: `TrafficLightsEnhancement/Systems/UI/UISystem.UIBIndings.cs`
- Reference: `TrafficLightsEnhancement/Systems/TrafficLightSystems/Simulation/PatchedTrafficLightSystem.cs`
- Reference: `TrafficLightsEnhancement/Systems/TrafficLightSystems/Simulation/TransitSignalPriorityRuntime.cs`
- Reference: `TrafficLightsEnhancement/Systems/TrafficGroupSystem.cs`
- Reference: `TrafficLightsEnhancement/Components/TransitSignalPrioritySettings.cs`
- Reference: `TrafficLightsEnhancement/UI/package.json`
- Reference: `TrafficLightsEnhancement.Tests/TrafficLightsEnhancement.Tests.csproj`

**Step 1: Confirm the TSP UI surface**

Run:

```bash
Select-String -Path 'TrafficLightsEnhancement/Systems/UI/UISystem.UIBIndings.cs' -Pattern 'TspEnabled|TspAllowTrackRequests|TspAllowPublicCarRequests|TspAllowGroupPropagation'
```

Expected: The binding code shows the exact TSP toggles exposed in the current UI.

**Step 2: Confirm runtime behavior**

Run:

```bash
Select-String -Path 'TrafficLightsEnhancement/Systems/TrafficLightSystems/Simulation/PatchedTrafficLightSystem.cs' -Pattern 'TryResolveActiveLocalRequest|TryGetCoordinatedGroupRequest|ApplySignalGroupOverride'
```

Expected: The simulation code shows both local TSP request handling and the non-custom signal-group override path.

**Step 3: Confirm group propagation behavior**

Run:

```bash
Select-String -Path 'TrafficLightsEnhancement/Systems/TrafficGroupSystem.cs' -Pattern 'm_TspPropagationEnabled|UpdateGroupTspState|AllowGroupPropagation'
```

Expected: The group system combines follower requests only when propagation is enabled.

### Task 2: Rewrite the README

**Files:**
- Modify: `README.md`

**Step 1: Replace the inherited intro**

Write a fork-first introduction that names `Cities2-TLE-TSP`, identifies the upstream repository, and states that this README focuses on verified fork behavior.

**Step 2: Add exact fork change sections**

Document:

```text
- confirmed fork-specific additions
- confirmed TSP behavior
- known limits and uncertainty
- build/install notes that are actually supported by this repository
```

**Step 3: Remove inherited claims that are no longer authoritative**

Delete or rewrite:

```text
- upstream release/distribution assumptions
- translation workflow calls that are not confirmed for this fork
- generic original-project framing that hides the fork identity
```

### Task 3: Verify scope and clarity

**Files:**
- Modify: `README.md`

**Step 1: Review the final diff**

Run:

```bash
git diff -- README.md
```

Expected: The diff shows a fork-first rewrite with exact TSP language and no accidental unrelated changes.

**Step 2: Check worktree status**

Run:

```bash
git status --short -- README.md docs/plans/2026-03-22-readme-fork-rewrite-design.md docs/plans/2026-03-22-readme-fork-rewrite.md
```

Expected: Only the README and the two plan documents are modified for this task.

**Step 3: Commit**

```bash
git add README.md docs/plans/2026-03-22-readme-fork-rewrite-design.md docs/plans/2026-03-22-readme-fork-rewrite.md
git commit -m "docs: rewrite README for fork-specific TSP behavior"
```
