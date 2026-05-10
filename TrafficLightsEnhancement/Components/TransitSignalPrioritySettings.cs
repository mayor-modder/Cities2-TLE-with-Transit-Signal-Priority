using Colossal.Serialization.Entities;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Components;

public struct TransitSignalPrioritySettings : IComponentData, ISerializable
{
    public bool m_Enabled;
    public bool m_AllowTrackRequests;
    public bool m_AllowPublicCarRequests;
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
        m_RequestHorizonTicks = global::TrafficLightsEnhancement.Logic.Tsp.TransitSignalPrioritySettings.DefaultRequestHorizonTicks;
        m_MaxGreenExtensionTicks = global::TrafficLightsEnhancement.Logic.Tsp.TransitSignalPrioritySettings.DefaultMaxGreenExtensionTicks;
    }

    public void Normalize()
    {
        m_RequestHorizonTicks = global::TrafficLightsEnhancement.Logic.Tsp.TspPolicy.GetEffectiveRequestHorizonTicks(m_RequestHorizonTicks);
        m_MaxGreenExtensionTicks = global::TrafficLightsEnhancement.Logic.Tsp.TspPolicy.GetEffectiveMaxGreenExtensionTicks(m_MaxGreenExtensionTicks);
    }

    public global::TrafficLightsEnhancement.Logic.Tsp.TransitSignalPrioritySettings ToLogicSettings()
    {
        return new global::TrafficLightsEnhancement.Logic.Tsp.TransitSignalPrioritySettings(
            enabled: m_Enabled,
            allowTrackRequests: m_AllowTrackRequests,
            allowPublicCarRequests: m_AllowPublicCarRequests,
            requestHorizonTicks: m_RequestHorizonTicks,
            maxGreenExtensionTicks: m_MaxGreenExtensionTicks);
    }

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write(2);
        writer.Write(m_Enabled);
        writer.Write(m_AllowTrackRequests);
        writer.Write(m_AllowPublicCarRequests);
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
        if (version == 1)
        {
            reader.Read(out bool _);
        }
        reader.Read(out m_RequestHorizonTicks);
        reader.Read(out m_MaxGreenExtensionTicks);

        Normalize();
    }
}
