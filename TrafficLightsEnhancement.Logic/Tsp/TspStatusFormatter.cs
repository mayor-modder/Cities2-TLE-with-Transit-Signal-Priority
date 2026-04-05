using System.Globalization;

namespace TrafficLightsEnhancement.Logic.Tsp;

public enum TspRequestOrigin : byte
{
    Local = 0,
    GroupedPropagation = 1,
}

public readonly struct TspStatusSnapshot
{
    public TspStatusSnapshot(
        bool enabled,
        bool hasRequest,
        TspSource source,
        TspRequestOrigin requestOrigin,
        int targetSignalGroup,
        TspSelectionReason reason)
    {
        Enabled = enabled;
        HasRequest = hasRequest;
        Source = source;
        RequestOrigin = requestOrigin;
        TargetSignalGroup = targetSignalGroup;
        Reason = reason;
    }

    public bool Enabled { get; }
    public bool HasRequest { get; }
    public TspSource Source { get; }
    public TspRequestOrigin RequestOrigin { get; }
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
            _ => "Request active",
        };

        string request = snapshot.Source switch
        {
            TspSource.Track => "Tram / Track",
            TspSource.PublicCar => "Bus Lane",
            _ => "Unknown",
        };

        if (snapshot.RequestOrigin == TspRequestOrigin.GroupedPropagation)
        {
            request += " (Propagated)";
        }

        string? targetSignalGroup = snapshot.TargetSignalGroup > 0
            ? snapshot.TargetSignalGroup.ToString(CultureInfo.InvariantCulture)
            : null;

        return new TspStatusPresentation(status, request, targetSignalGroup);
    }
}
