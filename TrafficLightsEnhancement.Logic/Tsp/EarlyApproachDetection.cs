namespace TrafficLightsEnhancement.Logic.Tsp;

[Flags]
public enum TransitApproachSuppressionFlags : byte
{
    None = 0,
    Boarding = 1 << 0,
    Arriving = 1 << 1,
    RequireStop = 1 << 2,
}

public struct TransitApproachScanState
{
    public TspRequest? EarlyRequest { get; set; }
    public TspRequest? PetitionerRequest { get; set; }
}

public static class EarlyApproachDetection
{
    public static TEntity ResolveApproachLane<TEntity>(TEntity signaledLane, TEntity sourceLane, TEntity nullLane)
        where TEntity : struct, IEquatable<TEntity>
    {
        return sourceLane.Equals(nullLane) ? signaledLane : sourceLane;
    }

    public static bool IsMovingEligibleApproachState(bool isEligibleLane, bool isVehicleMoving)
    {
        return IsMovingEligibleApproachState(
            isEligibleLane,
            isVehicleMoving,
            TransitApproachSuppressionFlags.None);
    }

    public static bool IsMovingEligibleApproachState(
        bool isEligibleLane,
        bool isVehicleMoving,
        TransitApproachSuppressionFlags suppressionFlags)
    {
        bool isSuppressed =
            (suppressionFlags & (TransitApproachSuppressionFlags.Boarding
                | TransitApproachSuppressionFlags.Arriving
                | TransitApproachSuppressionFlags.RequireStop)) != 0;

        return isEligibleLane && isVehicleMoving && !isSuppressed;
    }

    public static bool IsEligibleRoadTransitApproachState(
        bool isEligibleLane,
        bool hasReachedApproachThreshold,
        bool isBlocked,
        bool hasReachedLaneEnd,
        TransitApproachSuppressionFlags suppressionFlags)
    {
        bool isSuppressed =
            (suppressionFlags & (TransitApproachSuppressionFlags.Boarding
                | TransitApproachSuppressionFlags.Arriving
                | TransitApproachSuppressionFlags.RequireStop)) != 0;

        return isEligibleLane
            && hasReachedApproachThreshold
            && !isBlocked
            && !hasReachedLaneEnd
            && !isSuppressed;
    }

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

    public static TransitApproachScanState RecordLaneRequests(
        TransitApproachScanState state,
        TspRequest? earlyRequest,
        TspRequest? petitionerRequest)
    {
        return new TransitApproachScanState
        {
            EarlyRequest = state.EarlyRequest ?? earlyRequest,
            PetitionerRequest = state.PetitionerRequest ?? petitionerRequest,
        };
    }

    public static TspRequest? PreferEarlyRequest(TspRequest? earlyRequest, TspRequest? petitionerRequest)
    {
        return earlyRequest ?? petitionerRequest;
    }
}
