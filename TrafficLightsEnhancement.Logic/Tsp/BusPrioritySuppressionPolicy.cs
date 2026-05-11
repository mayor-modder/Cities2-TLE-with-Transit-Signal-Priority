namespace TrafficLightsEnhancement.Logic.Tsp;

public enum BusStopRelation : byte
{
    None = 0,
    NearSideBeforeSignal = 1,
    FarSideAfterSignal = 2,
    Unknown = 3,
}

public enum BusPrioritySuppressionReason : byte
{
    None = 0,
    Boarding = 1,
    NearSideStop = 2,
    UnknownStopRelation = 3,
}

public readonly struct BusPrioritySuppressionDecision
{
    public BusPrioritySuppressionDecision(bool isSuppressed, BusPrioritySuppressionReason reason)
    {
        IsSuppressed = isSuppressed;
        Reason = reason;
    }

    public bool IsSuppressed { get; }
    public BusPrioritySuppressionReason Reason { get; }
}

public static class BusPrioritySuppressionPolicy
{
    public static BusPrioritySuppressionDecision EvaluateStopSuppression(
        TransitApproachSuppressionFlags flags,
        BusStopRelation stopRelation)
    {
        if ((flags & TransitApproachSuppressionFlags.Boarding) != 0)
        {
            return new BusPrioritySuppressionDecision(true, BusPrioritySuppressionReason.Boarding);
        }

        bool isStopBound =
            (flags & (TransitApproachSuppressionFlags.Arriving | TransitApproachSuppressionFlags.RequireStop)) != 0;
        if (!isStopBound)
        {
            return new BusPrioritySuppressionDecision(false, BusPrioritySuppressionReason.None);
        }

        return stopRelation switch
        {
            BusStopRelation.NearSideBeforeSignal => new BusPrioritySuppressionDecision(
                true,
                BusPrioritySuppressionReason.NearSideStop),
            BusStopRelation.Unknown => new BusPrioritySuppressionDecision(
                true,
                BusPrioritySuppressionReason.UnknownStopRelation),
            _ => new BusPrioritySuppressionDecision(false, BusPrioritySuppressionReason.None),
        };
    }
}
