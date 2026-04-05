using TrafficLightsEnhancement.Logic.Tsp;
using Xunit;

namespace TrafficLightsEnhancement.Tests.Tsp;

public class TspStatusFormatterTests
{
    [Fact]
    public void Disabled_status_reports_disabled_without_request_details()
    {
        var presentation = TspStatusFormatter.Format(new TspStatusSnapshot(
            enabled: false,
            hasRequest: false,
            source: TspSource.None,
            requestOrigin: TspRequestOrigin.Local,
            targetSignalGroup: 0,
            reason: TspSelectionReason.None));

        Assert.Equal("Disabled", presentation.Status);
        Assert.Null(presentation.Request);
        Assert.Null(presentation.TargetSignalGroup);
    }

    [Fact]
    public void Extending_current_phase_reports_the_live_request_source()
    {
        var presentation = TspStatusFormatter.Format(new TspStatusSnapshot(
            enabled: true,
            hasRequest: true,
            source: TspSource.Track,
            requestOrigin: TspRequestOrigin.Local,
            targetSignalGroup: 2,
            reason: TspSelectionReason.ExtendedCurrentPhase));

        Assert.Equal("Extending current phase", presentation.Status);
        Assert.Equal("Tram / Track", presentation.Request);
        Assert.Equal("2", presentation.TargetSignalGroup);
    }

    [Fact]
    public void Grouped_propagation_status_marks_the_request_as_propagated()
    {
        var presentation = TspStatusFormatter.Format(new TspStatusSnapshot(
            enabled: true,
            hasRequest: true,
            source: TspSource.PublicCar,
            requestOrigin: TspRequestOrigin.GroupedPropagation,
            targetSignalGroup: 4,
            reason: TspSelectionReason.SelectedTargetPhase));

        Assert.Equal("Switching to requested group", presentation.Status);
        Assert.Equal("Bus Lane (Propagated)", presentation.Request);
        Assert.Equal("4", presentation.TargetSignalGroup);
    }
}
