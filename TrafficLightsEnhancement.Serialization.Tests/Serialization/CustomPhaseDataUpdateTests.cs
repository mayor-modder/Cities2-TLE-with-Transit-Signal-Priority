using C2VM.TrafficLightsEnhancement.Components;
using C2VM.TrafficLightsEnhancement.Utils;
using Xunit;

namespace TrafficLightsEnhancement.Serialization.Tests.Serialization;

public class CustomPhaseDataUpdateTests
{
    [Theory]
    [InlineData("CarWeight", 1.25f, "car")]
    [InlineData("PublicCarWeight", 2.25f, "publicCar")]
    [InlineData("TrackWeight", 3.25f, "track")]
    [InlineData("PedestrianWeight", 4.25f, "pedestrian")]
    [InlineData("BicycleWeight", 5.25f, "bicycle")]
    [InlineData("SmoothingFactor", 0.75f, "smoothing")]
    public void Apply_updates_dynamic_weight_and_smoothing_fields(string key, float value, string field)
    {
        var phase = new CustomPhaseData();

        bool updated = CustomPhaseDataUpdate.TryApply(key, value, ref phase);

        Assert.True(updated);
        Assert.Equal(field == "car" ? value : 1.0f, phase.m_CarWeight);
        Assert.Equal(field == "publicCar" ? value : 2.0f, phase.m_PublicCarWeight);
        Assert.Equal(field == "track" ? value : 3.0f, phase.m_TrackWeight);
        Assert.Equal(field == "pedestrian" ? value : 1.0f, phase.m_PedestrianWeight);
        Assert.Equal(field == "bicycle" ? value : 1.0f, phase.m_BicycleWeight);
        Assert.Equal(field == "smoothing" ? value : 0.5f, phase.m_SmoothingFactor);
    }

    [Fact]
    public void Apply_preserves_minimum_maximum_duration_invariant()
    {
        var phase = new CustomPhaseData
        {
            m_MinimumDuration = 10,
            m_MaximumDuration = 20
        };

        CustomPhaseDataUpdate.TryApply("MinimumDuration", 30, ref phase);

        Assert.Equal((ushort)30, phase.m_MinimumDuration);
        Assert.Equal((ushort)30, phase.m_MaximumDuration);
    }

    [Fact]
    public void Apply_toggles_linked_phase_option()
    {
        var phase = new CustomPhaseData();

        CustomPhaseDataUpdate.TryApply("LinkedWithNextPhase", true, ref phase);

        Assert.True((phase.m_Options & CustomPhaseData.Options.LinkedWithNextPhase) != 0);
    }
}
