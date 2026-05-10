namespace TrafficLightsEnhancement.Logic.Tsp;

public static class TransitSignalPriorityRuntime
{
    public static bool TryBuildRequestForLane(
        TransitSignalPrioritySettings? settings,
        bool isTrackLane,
        out TspRequest request)
    {
        if (!settings.HasValue)
        {
            request = default;
            return false;
        }

        return TryBuildRequestForLane(settings.Value, isTrackLane, out request);
    }

    public static bool TryBuildRequestForLane(
        TransitSignalPrioritySettings settings,
        bool isTrackLane,
        out TspRequest request)
    {
        request = default;

        if (!settings.m_Enabled)
        {
            return false;
        }

        if (isTrackLane && settings.m_AllowTrackRequests)
        {
            request = new TspRequest(source: TspSource.Track, strength: 1f, extensionEligible: true);
            return true;
        }

        return false;
    }
}
