using TrafficLightsEnhancement.Logic.Tsp;
using Xunit;

namespace TrafficLightsEnhancement.Tests.Tsp;

public class TspStatusFormatterTests
{
    [Fact]
    public void FeatureLabel_UsesTramSignalPriorityLanguage()
    {
        Assert.Contains("Tram", TspStatusFormatter.FeatureLabel);
        Assert.DoesNotContain("Bus", TspStatusFormatter.FeatureLabel);
    }

    [Fact]
    public void Disabled_status_reports_disabled_without_request_details()
    {
        var presentation = TspStatusFormatter.Format(new TspStatusSnapshot(
            enabled: false,
            hasRequest: false,
            source: TspSource.None,
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
            targetSignalGroup: 2,
            reason: TspSelectionReason.ExtendedCurrentPhase));

        Assert.Equal("Extending current phase", presentation.Status);
        Assert.Equal("Tram / Track", presentation.Request);
        Assert.Equal("2", presentation.TargetSignalGroup);
    }

}
