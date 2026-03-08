using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Components;

public struct TrafficGroupTspState : IComponentData
{
    public byte m_TargetSignalGroup;
    public byte m_SourceType;
    public float m_Strength;
    public uint m_ExpiryTimer;
    public bool m_ExtendCurrentPhase;
}
