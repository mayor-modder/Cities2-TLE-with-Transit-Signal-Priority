namespace TrafficLightsEnhancement.Logic.Tsp;

public readonly struct TspSignalRequest
{
    public TspSignalRequest(int targetSignalGroup, TspSource source, float strength, uint expiryTimer, bool extendCurrentPhase)
    {
        TargetSignalGroup = targetSignalGroup;
        Source = source;
        Strength = strength;
        ExpiryTimer = expiryTimer;
        ExtendCurrentPhase = extendCurrentPhase;
    }

    public int TargetSignalGroup { get; }
    public TspSource Source { get; }
    public float Strength { get; }
    public uint ExpiryTimer { get; }
    public bool ExtendCurrentPhase { get; }
}

public static class TspPreemptionPolicy
{
    public const uint ReleaseGraceTicks = 6;
    public const int AggressivePreemptionMinimumGreenTicks = 1;

    public static bool TryRefreshOrLatchRequest(
        TspSignalRequest? freshRequest,
        TspSignalRequest? existingRequest,
        ushort requestHorizonTicks,
        byte currentSignalGroup,
        out TspSignalRequest request)
    {
        if (freshRequest.HasValue)
        {
            TspSignalRequest fresh = freshRequest.Value;
            if (!IsEligibleTrackRequest(fresh))
            {
                request = default;
                return false;
            }

            request = new TspSignalRequest(
                fresh.TargetSignalGroup,
                fresh.Source,
                fresh.Strength,
                requestHorizonTicks,
                fresh.ExtendCurrentPhase);
            return true;
        }

        if (existingRequest.HasValue)
        {
            TspSignalRequest existing = existingRequest.Value;
            if (IsEligibleTrackRequest(existing)
                && existing.ExpiryTimer > 0
                && existing.ExpiryTimer <= requestHorizonTicks)
            {
                request = new TspSignalRequest(
                    existing.TargetSignalGroup,
                    existing.Source,
                    existing.Strength,
                    existing.ExpiryTimer - 1,
                    existing.ExtendCurrentPhase);
                return true;
            }
        }

        request = default;
        return false;
    }

    private static bool IsEligibleTrackRequest(TspSignalRequest request)
    {
        return request.Source == TspSource.Track
            && request.TargetSignalGroup > 0
            && request.Strength > 0f;
    }

    public static bool ShouldHoldCurrentGroup(
        byte currentSignalGroup,
        TspSignalRequest request,
        uint signalTimer,
        ushort maxGreenExtensionTicks)
    {
        return request.Source == TspSource.Track
            && currentSignalGroup > 0
            && request.TargetSignalGroup == currentSignalGroup
            && request.ExtendCurrentPhase
            && request.ExpiryTimer > 0
            && signalTimer < maxGreenExtensionTicks;
    }

    public static int GetMinimumGreenDurationTicks(
        int defaultMinimumGreenTicks,
        byte currentSignalGroup,
        TspSignalRequest request,
        bool protectActivePedestrianPhase = false)
    {
        return IsTrackPreemptionToDifferentGroup(currentSignalGroup, request, protectActivePedestrianPhase)
            ? AggressivePreemptionMinimumGreenTicks
            : defaultMinimumGreenTicks;
    }

    public static bool ShouldAggressivelyPreemptToConflictingGroup(
        byte currentSignalGroup,
        TspSignalRequest request,
        bool protectActivePedestrianPhase = false)
    {
        return IsTrackPreemptionToDifferentGroup(currentSignalGroup, request, protectActivePedestrianPhase);
    }

    private static bool IsTrackPreemptionToDifferentGroup(
        byte currentSignalGroup,
        TspSignalRequest request,
        bool protectActivePedestrianPhase)
    {
        return !protectActivePedestrianPhase
            && currentSignalGroup > 0
            && request.Source == TspSource.Track
            && request.TargetSignalGroup > 0
            && request.TargetSignalGroup != currentSignalGroup
            && request.ExpiryTimer > 0;
    }

    public static bool ShouldProtectActivePedestrianPhase(
        bool exclusivePedestrianEnabled,
        byte currentSignalGroup,
        int pedestrianPhaseGroupMask,
        bool isOngoing)
    {
        return exclusivePedestrianEnabled
            && isOngoing
            && currentSignalGroup > 0
            && currentSignalGroup <= 32
            && (pedestrianPhaseGroupMask & (1 << (currentSignalGroup - 1))) != 0;
    }
}
