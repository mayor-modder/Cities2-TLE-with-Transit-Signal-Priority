namespace TrafficLightsEnhancement.Logic.Tsp;

public readonly struct TransitSignalPrioritySettings
{
    public const ushort DefaultRequestHorizonTicks = 10;
    public const ushort LegacyDefaultRequestHorizonTicks = 120;
    public const ushort MaxRequestHorizonTicksUpperBound = 120;
    public const ushort DefaultMaxGreenExtensionTicks = 45;
    public const ushort MaxGreenExtensionTicksUpperBound = 600;

    public readonly bool m_Enabled;
    public readonly bool m_AllowTrackRequests;
    public readonly bool m_AllowPublicCarRequests;
    public readonly ushort m_RequestHorizonTicks;
    public readonly ushort m_MaxGreenExtensionTicks;

    public TransitSignalPrioritySettings()
        : this(
            enabled: false,
            allowTrackRequests: true,
            allowPublicCarRequests: false,
            requestHorizonTicks: DefaultRequestHorizonTicks,
            maxGreenExtensionTicks: DefaultMaxGreenExtensionTicks)
    {
    }

    public TransitSignalPrioritySettings(
        bool enabled = false,
        bool allowTrackRequests = true,
        bool allowPublicCarRequests = false,
        ushort requestHorizonTicks = DefaultRequestHorizonTicks,
        ushort maxGreenExtensionTicks = DefaultMaxGreenExtensionTicks)
    {
        m_Enabled = enabled;
        m_AllowTrackRequests = allowTrackRequests;
        m_AllowPublicCarRequests = allowPublicCarRequests;
        m_RequestHorizonTicks = NormalizeRequestHorizonTicks(requestHorizonTicks);
        m_MaxGreenExtensionTicks = NormalizeMaxGreenExtensionTicks(maxGreenExtensionTicks);
    }

    public static ushort NormalizeRequestHorizonTicks(ushort requestHorizonTicks)
    {
        if (requestHorizonTicks == 0 || requestHorizonTicks == LegacyDefaultRequestHorizonTicks)
        {
            return DefaultRequestHorizonTicks;
        }

        return requestHorizonTicks > MaxRequestHorizonTicksUpperBound
            ? MaxRequestHorizonTicksUpperBound
            : requestHorizonTicks;
    }

    public static ushort NormalizeMaxGreenExtensionTicks(ushort maxGreenExtensionTicks)
    {
        if (maxGreenExtensionTicks == 0)
        {
            return DefaultMaxGreenExtensionTicks;
        }

        return maxGreenExtensionTicks > MaxGreenExtensionTicksUpperBound
            ? MaxGreenExtensionTicksUpperBound
            : maxGreenExtensionTicks;
    }
}
