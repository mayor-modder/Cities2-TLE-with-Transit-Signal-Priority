using System.Globalization;

namespace TrafficLightsEnhancement.Logic.Tsp;

public readonly struct TspStatusSnapshot
{
    public TspStatusSnapshot(
        bool enabled,
        bool hasRequest,
        TspSource source,
        int targetSignalGroup,
        TspSelectionReason reason)
    {
        Enabled = enabled;
        HasRequest = hasRequest;
        Source = source;
        TargetSignalGroup = targetSignalGroup;
        Reason = reason;
    }

    public bool Enabled { get; }
    public bool HasRequest { get; }
    public TspSource Source { get; }
    public int TargetSignalGroup { get; }
    public TspSelectionReason Reason { get; }
}

public readonly struct TspStatusPresentation
{
    public TspStatusPresentation(string status, string? request, string? targetSignalGroup)
    {
        Status = status;
        Request = request;
        TargetSignalGroup = targetSignalGroup;
    }

    public string Status { get; }
    public string? Request { get; }
    public string? TargetSignalGroup { get; }
}

public static class TspStatusFormatter
{
    public const string FeatureLabel = "Tram Signal Priority";

    public static TspStatusPresentation Format(TspStatusSnapshot snapshot)
    {
        if (!snapshot.Enabled)
        {
            return new TspStatusPresentation("Disabled", request: null, targetSignalGroup: null);
        }

        if (!snapshot.HasRequest || snapshot.Source == TspSource.None)
        {
            return new TspStatusPresentation("Idle", request: null, targetSignalGroup: null);
        }

        string status = snapshot.Reason switch
        {
            TspSelectionReason.ExtendedCurrentPhase => "Extending current phase",
            TspSelectionReason.SelectedTargetPhase => "Switching to requested group",
            TspSelectionReason.DeferredForPedestrianFairness => "Deferring for pedestrian phase",
            _ => "Request active",
        };

        string request = snapshot.Source switch
        {
            TspSource.Track => "Tram / Track",
            _ => "Unknown",
        };

        string? targetSignalGroup = snapshot.TargetSignalGroup > 0
            ? snapshot.TargetSignalGroup.ToString(CultureInfo.InvariantCulture)
            : null;

        return new TspStatusPresentation(status, request, targetSignalGroup);
    }
}
