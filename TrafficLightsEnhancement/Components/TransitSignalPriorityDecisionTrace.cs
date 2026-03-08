using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Components;

public struct TransitSignalPriorityDecisionTrace : IComponentData
{
    public byte m_RequestTargetSignalGroup;
    public byte m_SelectedSignalGroup;
    public byte m_BaseSignalGroup;
    public byte m_SourceType;
    public byte m_Reason;
    public bool m_FromCoordinatedGroup;
}
