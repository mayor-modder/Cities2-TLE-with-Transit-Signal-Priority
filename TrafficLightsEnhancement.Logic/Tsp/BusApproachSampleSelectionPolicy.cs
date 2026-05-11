namespace TrafficLightsEnhancement.Logic.Tsp;

public readonly struct BusApproachSampleSelectionInput
{
    public BusApproachSampleSelectionInput(
        float curvePosition,
        bool isBusOnlyLane,
        bool hasChangeLane,
        bool isChangeLaneSample,
        bool hasNavigation,
        byte navigationLaneCount)
    {
        CurvePosition = curvePosition;
        IsBusOnlyLane = isBusOnlyLane;
        HasChangeLane = hasChangeLane;
        IsChangeLaneSample = isChangeLaneSample;
        HasNavigation = hasNavigation;
        NavigationLaneCount = navigationLaneCount;
    }

    public float CurvePosition { get; }

    public bool IsBusOnlyLane { get; }

    public bool HasChangeLane { get; }

    public bool IsChangeLaneSample { get; }

    public bool HasNavigation { get; }

    public byte NavigationLaneCount { get; }
}

public readonly struct BusApproachSampleSelectionState
{
    public BusApproachSampleSelectionState(
        bool hasSample,
        float curvePosition,
        byte hitCount,
        bool isBusOnlyLane,
        bool hasChangeLane,
        bool isChangeLaneSample,
        bool hasNavigation,
        byte navigationLaneCount)
    {
        HasSample = hasSample;
        CurvePosition = curvePosition;
        HitCount = hitCount;
        IsBusOnlyLane = isBusOnlyLane;
        HasChangeLane = hasChangeLane;
        IsChangeLaneSample = isChangeLaneSample;
        HasNavigation = hasNavigation;
        NavigationLaneCount = navigationLaneCount;
    }

    public bool HasSample { get; }

    public float CurvePosition { get; }

    public byte HitCount { get; }

    public bool IsBusOnlyLane { get; }

    public bool HasChangeLane { get; }

    public bool IsChangeLaneSample { get; }

    public bool HasNavigation { get; }

    public byte NavigationLaneCount { get; }
}

public static class BusApproachSampleSelectionPolicy
{
    public static BusApproachSampleSelectionState RecordSample(
        BusApproachSampleSelectionState existing,
        BusApproachSampleSelectionInput sample)
    {
        byte hitCount = existing.HasSample ? IncrementByte(existing.HitCount) : (byte)1;

        if (existing.HasSample && existing.CurvePosition > sample.CurvePosition)
        {
            return new BusApproachSampleSelectionState(
                hasSample: true,
                curvePosition: existing.CurvePosition,
                hitCount,
                isBusOnlyLane: existing.IsBusOnlyLane,
                hasChangeLane: existing.HasChangeLane || sample.HasChangeLane,
                isChangeLaneSample: existing.IsChangeLaneSample,
                hasNavigation: existing.HasNavigation || sample.HasNavigation,
                navigationLaneCount: Max(existing.NavigationLaneCount, sample.NavigationLaneCount));
        }

        return new BusApproachSampleSelectionState(
            hasSample: true,
            curvePosition: sample.CurvePosition,
            hitCount,
            isBusOnlyLane: sample.IsBusOnlyLane,
            hasChangeLane: sample.HasChangeLane,
            isChangeLaneSample: sample.IsChangeLaneSample,
            hasNavigation: sample.HasNavigation,
            navigationLaneCount: sample.NavigationLaneCount);
    }

    public static byte ToByteCount(int value)
    {
        return value >= byte.MaxValue ? byte.MaxValue : (byte)value;
    }

    private static byte IncrementByte(byte value)
    {
        return value == byte.MaxValue ? byte.MaxValue : (byte)(value + 1);
    }

    private static byte Max(byte left, byte right)
    {
        return left > right ? left : right;
    }
}
