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
            if (!IsEligibleRequest(fresh))
            {
                request = default;
                return false;
            }

            if (existingRequest.HasValue
                && IsValidLatchedRequest(existingRequest.Value, requestHorizonTicks)
                && GetSourcePriority(existingRequest.Value.Source) > GetSourcePriority(fresh.Source))
            {
                request = DecrementLatchedRequest(existingRequest.Value);
                return true;
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
            if (IsValidLatchedRequest(existing, requestHorizonTicks))
            {
                request = DecrementLatchedRequest(existing);
                return true;
            }
        }

        request = default;
        return false;
    }

    private static bool IsEligibleRequest(TspSignalRequest request)
    {
        return request.Source is TspSource.Track or TspSource.PublicCar
            && request.TargetSignalGroup > 0
            && request.Strength > 0f;
    }

    private static bool IsValidLatchedRequest(TspSignalRequest request, ushort requestHorizonTicks)
    {
        return IsEligibleRequest(request)
            && request.ExpiryTimer > 0
            && request.ExpiryTimer <= requestHorizonTicks;
    }

    private static TspSignalRequest DecrementLatchedRequest(TspSignalRequest request)
    {
        return new TspSignalRequest(
            request.TargetSignalGroup,
            request.Source,
            request.Strength,
            request.ExpiryTimer - 1,
            request.ExtendCurrentPhase);
    }

    private static int GetSourcePriority(TspSource source)
    {
        return source switch
        {
            TspSource.Track => 2,
            TspSource.PublicCar => 1,
            _ => 0,
        };
    }

    public static bool ShouldHoldCurrentGroup(
        byte currentSignalGroup,
        TspSignalRequest request,
        uint signalTimer,
        ushort maxGreenExtensionTicks)
    {
        return request.Source is TspSource.Track or TspSource.PublicCar
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

    public static bool ShouldApplyTargetGroupSelection(
        TspSignalRequest request,
        bool protectActivePedestrianPhase = false)
    {
        return !protectActivePedestrianPhase
            && IsEligibleRequest(request)
            && request.ExpiryTimer > 0;
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
