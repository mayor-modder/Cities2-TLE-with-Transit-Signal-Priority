using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Components;

public struct TransitSignalPriorityDebugState : IComponentData
{
    public bool m_HadRequest;
    public byte m_TargetSignalGroup;
    public byte m_SourceType;
    public float m_Strength;
    public bool m_ExtendCurrentPhase;
}
