using TrafficLightsEnhancement.Logic.Tsp;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Components;

public struct TransitSignalPriorityVehicleFairnessState : IComponentData
{
    public byte m_PendingVehicleSignalGroup;

    public readonly TspVehicleFairnessState ToLogicState()
    {
        return new TspVehicleFairnessState(m_PendingVehicleSignalGroup);
    }

    public static TransitSignalPriorityVehicleFairnessState FromLogicState(TspVehicleFairnessState state)
    {
        return new TransitSignalPriorityVehicleFairnessState
        {
            m_PendingVehicleSignalGroup = state.PendingVehicleSignalGroup,
        };
    }
}
