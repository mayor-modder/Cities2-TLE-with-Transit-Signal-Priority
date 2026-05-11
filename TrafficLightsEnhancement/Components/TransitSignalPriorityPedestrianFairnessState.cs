using TrafficLightsEnhancement.Logic.Tsp;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Components;

public struct TransitSignalPriorityPedestrianFairnessState : IComponentData
{
    public byte m_PendingPedestrianSignalGroup;

    public readonly TspPedestrianFairnessState ToLogicState()
    {
        return new TspPedestrianFairnessState(m_PendingPedestrianSignalGroup);
    }

    public static TransitSignalPriorityPedestrianFairnessState FromLogicState(TspPedestrianFairnessState state)
    {
        return new TransitSignalPriorityPedestrianFairnessState
        {
            m_PendingPedestrianSignalGroup = state.PendingPedestrianSignalGroup,
        };
    }
}
