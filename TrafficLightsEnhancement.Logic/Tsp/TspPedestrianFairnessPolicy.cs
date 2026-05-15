namespace TrafficLightsEnhancement.Logic.Tsp;

public readonly struct TspPedestrianFairnessState
{
    public static TspPedestrianFairnessState None => default;

    public TspPedestrianFairnessState(byte pendingPedestrianSignalGroup)
    {
        PendingPedestrianSignalGroup = pendingPedestrianSignalGroup;
    }

    public byte PendingPedestrianSignalGroup { get; }
    public bool HasPendingPedestrianPhase => PendingPedestrianSignalGroup > 0;
}

public static class TspPedestrianFairnessPolicy
{
    public static TspPedestrianFairnessState Refresh(
        TspPedestrianFairnessState state,
        bool exclusivePedestrianEnabled,
        int pedestrianPhaseGroupMask,
        byte currentSignalGroup)
    {
        if (!exclusivePedestrianEnabled || !state.HasPendingPedestrianPhase)
        {
            return TspPedestrianFairnessState.None;
        }

        if (!IsPedestrianPhase(state.PendingPedestrianSignalGroup, pedestrianPhaseGroupMask))
        {
            return TspPedestrianFairnessState.None;
        }

        return currentSignalGroup == state.PendingPedestrianSignalGroup
            ? TspPedestrianFairnessState.None
            : state;
    }

    public static bool ShouldDeferToPendingPedestrianPhase(
        TspPedestrianFairnessState state,
        bool exclusivePedestrianEnabled,
        int pedestrianPhaseGroupMask,
        byte currentSignalGroup,
        byte requestedSignalGroup,
        byte inFlightSignalGroup = 0)
    {
        state = Refresh(state, exclusivePedestrianEnabled, pedestrianPhaseGroupMask, currentSignalGroup);
        return state.HasPendingPedestrianPhase
            && (inFlightSignalGroup == 0 || inFlightSignalGroup == state.PendingPedestrianSignalGroup)
            && requestedSignalGroup != state.PendingPedestrianSignalGroup;
    }

    public static bool ShouldSuppressCurrentGroupHold(
        TspPedestrianFairnessState state,
        bool exclusivePedestrianEnabled,
        int pedestrianPhaseGroupMask,
        byte currentSignalGroup)
    {
        return ShouldDeferToPendingPedestrianPhase(
            state,
            exclusivePedestrianEnabled,
            pedestrianPhaseGroupMask,
            currentSignalGroup,
            requestedSignalGroup: currentSignalGroup);
    }

    public static TspPedestrianFairnessState UpdateAfterSelection(
        TspPedestrianFairnessState state,
        bool exclusivePedestrianEnabled,
        int pedestrianPhaseGroupMask,
        byte currentSignalGroup,
        byte baseSignalGroup,
        byte selectedSignalGroup,
        bool tspOverrideApplied)
    {
        state = Refresh(state, exclusivePedestrianEnabled, pedestrianPhaseGroupMask, currentSignalGroup);
        if (state.HasPendingPedestrianPhase || !tspOverrideApplied)
        {
            return state;
        }

        if (IsPedestrianPhase(baseSignalGroup, pedestrianPhaseGroupMask)
            && selectedSignalGroup != baseSignalGroup)
        {
            return new TspPedestrianFairnessState(baseSignalGroup);
        }

        return state;
    }

    private static bool IsPedestrianPhase(byte signalGroup, int pedestrianPhaseGroupMask)
    {
        return signalGroup > 0
            && signalGroup <= 32
            && (pedestrianPhaseGroupMask & (1 << (signalGroup - 1))) != 0;
    }
}
