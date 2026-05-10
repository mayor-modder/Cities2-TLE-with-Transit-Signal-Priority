using Colossal.Serialization.Entities;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Components;

public struct TransitSignalPrioritySettings : IComponentData, ISerializable
{
    public bool m_Enabled;
    public bool m_AllowTrackRequests;
    public bool m_AllowPublicCarRequests;
    public bool m_AllowGroupPropagation;
    public ushort m_RequestHorizonTicks;
    public ushort m_MaxGreenExtensionTicks;

    public TransitSignalPrioritySettings()
    {
        this = default;
        ResetDefaults();
    }

    public static TransitSignalPrioritySettings CreateDefault()
    {
        var settings = default(TransitSignalPrioritySettings);
        settings.ResetDefaults();
        return settings;
    }

    public void ResetDefaults()
    {
        m_Enabled = false;
        m_AllowTrackRequests = true;
        m_AllowPublicCarRequests = false;
        m_AllowGroupPropagation = false;
        m_RequestHorizonTicks = global::TrafficLightsEnhancement.Logic.Tsp.TransitSignalPrioritySettings.DefaultRequestHorizonTicks;
        m_MaxGreenExtensionTicks = global::TrafficLightsEnhancement.Logic.Tsp.TransitSignalPrioritySettings.DefaultMaxGreenExtensionTicks;
    }

    public void Normalize()
    {
        m_AllowTrackRequests = true;
        m_AllowPublicCarRequests = false;
        m_AllowGroupPropagation = false;
        m_RequestHorizonTicks = global::TrafficLightsEnhancement.Logic.Tsp.TspPolicy.GetEffectiveRequestHorizonTicks(m_RequestHorizonTicks);
        m_MaxGreenExtensionTicks = global::TrafficLightsEnhancement.Logic.Tsp.TspPolicy.GetEffectiveMaxGreenExtensionTicks(m_MaxGreenExtensionTicks);
    }

    public global::TrafficLightsEnhancement.Logic.Tsp.TransitSignalPrioritySettings ToLogicSettings()
    {
        return new global::TrafficLightsEnhancement.Logic.Tsp.TransitSignalPrioritySettings(
            enabled: m_Enabled,
            allowTrackRequests: m_AllowTrackRequests,
            allowPublicCarRequests: m_AllowPublicCarRequests,
            allowGroupPropagation: m_AllowGroupPropagation,
            requestHorizonTicks: m_RequestHorizonTicks,
            maxGreenExtensionTicks: m_MaxGreenExtensionTicks);
    }

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(1);
        writer.Write(m_Enabled);
        writer.Write(m_AllowTrackRequests);
        writer.Write(m_AllowPublicCarRequests);
        writer.Write(m_AllowGroupPropagation);
        writer.Write(m_RequestHorizonTicks);
        writer.Write(m_MaxGreenExtensionTicks);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        this = CreateDefault();

        reader.Read(out int version);
        if (version < 1)
        {
            return;
        }

        reader.Read(out m_Enabled);
        reader.Read(out m_AllowTrackRequests);
        reader.Read(out m_AllowPublicCarRequests);
        reader.Read(out m_AllowGroupPropagation);
        reader.Read(out m_RequestHorizonTicks);
        reader.Read(out m_MaxGreenExtensionTicks);

        // Compatibility rule: persisted PublicCar fields remain readable, but this MVP
        // forces tram-only behavior after deserialize.
        Normalize();
    }
}
