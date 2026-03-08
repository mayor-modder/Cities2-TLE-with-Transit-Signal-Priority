namespace TrafficLightsEnhancement.Logic.Tsp;

public enum TspSelectionReason : byte
{
    None = 0,
    ExtendedCurrentPhase = 1,
    SelectedTargetPhase = 2,
}

public readonly struct TspOverrideSelection
{
    public TspOverrideSelection(int basePhaseIndex, int selectedPhaseIndex, bool canExtendCurrent, TspSelectionReason reason)
    {
        BasePhaseIndex = basePhaseIndex;
        SelectedPhaseIndex = selectedPhaseIndex;
        CanExtendCurrent = canExtendCurrent;
        Reason = reason;
    }

    public int BasePhaseIndex { get; }
    public int SelectedPhaseIndex { get; }
    public bool CanExtendCurrent { get; }
    public TspSelectionReason Reason { get; }
    public bool Applied => Reason != TspSelectionReason.None;
    public bool ChangedBaseSelection => BasePhaseIndex != SelectedPhaseIndex;
}

public static class TspOverrideEngine
{
    public static TspOverrideSelection ApplySignalGroupOverride(
        int baseSignalGroup,
        int currentSignalGroup,
        int signalGroupCount,
        int targetSignalGroup,
        TspRequest request)
    {
        return ApplyRequestOverride(
            basePhaseIndex: baseSignalGroup > 0 ? baseSignalGroup - 1 : -1,
            currentPhaseIndex: currentSignalGroup > 0 ? currentSignalGroup - 1 : -1,
            phaseCount: signalGroupCount,
            targetPhaseIndex: targetSignalGroup > 0 ? targetSignalGroup - 1 : -1,
            request);
    }

    public static TspOverrideSelection ApplyRequestOverride(
        int basePhaseIndex,
        int currentPhaseIndex,
        int phaseCount,
        int targetPhaseIndex,
        TspRequest request)
    {
        if (phaseCount <= 0)
        {
            return new TspOverrideSelection(basePhaseIndex, basePhaseIndex, canExtendCurrent: false, TspSelectionReason.None);
        }

        if (request.ExtensionEligible && currentPhaseIndex >= 0 && currentPhaseIndex < phaseCount)
        {
            return new TspOverrideSelection(
                basePhaseIndex,
                currentPhaseIndex,
                canExtendCurrent: true,
                TspSelectionReason.ExtendedCurrentPhase);
        }

        if (request.Source != TspSource.None && targetPhaseIndex >= 0 && targetPhaseIndex < phaseCount)
        {
            return new TspOverrideSelection(
                basePhaseIndex,
                targetPhaseIndex,
                canExtendCurrent: false,
                TspSelectionReason.SelectedTargetPhase);
        }

        return new TspOverrideSelection(basePhaseIndex, basePhaseIndex, canExtendCurrent: false, TspSelectionReason.None);
    }
}
