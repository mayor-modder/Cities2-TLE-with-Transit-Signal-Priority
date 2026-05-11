namespace TrafficLightsEnhancement.Logic.Tsp;

public readonly struct TspVehicleFairnessState
{
    public static TspVehicleFairnessState None => default;

    public TspVehicleFairnessState(byte pendingVehicleSignalGroup)
    {
        PendingVehicleSignalGroup = pendingVehicleSignalGroup;
    }

    public byte PendingVehicleSignalGroup { get; }
    public bool HasPendingVehiclePhase => PendingVehicleSignalGroup > 0;
}

public static class TspVehicleFairnessPolicy
{
    public static TspVehicleFairnessState Refresh(
        TspVehicleFairnessState state,
        int signalGroupCount,
        byte currentSignalGroup)
    {
        if (!state.HasPendingVehiclePhase
            || state.PendingVehicleSignalGroup > signalGroupCount)
        {
            return TspVehicleFairnessState.None;
        }

        return currentSignalGroup == state.PendingVehicleSignalGroup
            ? TspVehicleFairnessState.None
            : state;
    }

    public static bool ShouldDeferToPendingVehiclePhase(
        TspVehicleFairnessState state,
        int signalGroupCount,
        byte currentSignalGroup,
        byte baseSignalGroup,
        byte requestedSignalGroup,
        byte inFlightSignalGroup = 0)
    {
        state = Refresh(state, signalGroupCount, currentSignalGroup);
        return state.HasPendingVehiclePhase
            && requestedSignalGroup != state.PendingVehicleSignalGroup
            && (baseSignalGroup == state.PendingVehicleSignalGroup
                || inFlightSignalGroup == state.PendingVehicleSignalGroup);
    }

    public static TspVehicleFairnessState UpdateAfterSelection(
        TspVehicleFairnessState state,
        int signalGroupCount,
        byte currentSignalGroup,
        byte baseSignalGroup,
        byte selectedSignalGroup,
        bool tspOverrideApplied)
    {
        state = Refresh(state, signalGroupCount, currentSignalGroup);
        if (state.HasPendingVehiclePhase || !tspOverrideApplied)
        {
            return state;
        }

        if (baseSignalGroup > 0
            && baseSignalGroup <= signalGroupCount
            && selectedSignalGroup != baseSignalGroup)
        {
            return new TspVehicleFairnessState(baseSignalGroup);
        }

        return state;
    }
}
