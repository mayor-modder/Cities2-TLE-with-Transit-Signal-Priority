using TrafficLightsEnhancement.Logic.Tsp;
using Xunit;

namespace TrafficLightsEnhancement.Tests.Tsp;

public class BusApproachSampleSelectionPolicyTests
{
    [Fact]
    public void First_sample_becomes_selected_sample()
    {
        BusApproachSampleSelectionState state = BusApproachSampleSelectionPolicy.RecordSample(
            default,
            new BusApproachSampleSelectionInput(
                curvePosition: 0.42f,
                isBusOnlyLane: true,
                hasChangeLane: true,
                isChangeLaneSample: false,
                hasNavigation: true,
                navigationLaneCount: 3));

        Assert.True(state.HasSample);
        Assert.Equal(0.42f, state.CurvePosition);
        Assert.Equal(1, state.HitCount);
        Assert.True(state.IsBusOnlyLane);
        Assert.True(state.HasChangeLane);
        Assert.False(state.IsChangeLaneSample);
        Assert.True(state.HasNavigation);
        Assert.Equal(3, state.NavigationLaneCount);
    }

    [Fact]
    public void Farther_sample_keeps_existing_selected_bus_but_aggregates_count_and_observed_flags()
    {
        BusApproachSampleSelectionState state = BusApproachSampleSelectionPolicy.RecordSample(
            default,
            new BusApproachSampleSelectionInput(
                curvePosition: 0.80f,
                isBusOnlyLane: false,
                hasChangeLane: false,
                isChangeLaneSample: false,
                hasNavigation: false,
                navigationLaneCount: 0));

        state = BusApproachSampleSelectionPolicy.RecordSample(
            state,
            new BusApproachSampleSelectionInput(
                curvePosition: 0.25f,
                isBusOnlyLane: true,
                hasChangeLane: true,
                isChangeLaneSample: true,
                hasNavigation: true,
                navigationLaneCount: 4));

        Assert.Equal(0.80f, state.CurvePosition);
        Assert.Equal(2, state.HitCount);
        Assert.False(state.IsBusOnlyLane);
        Assert.True(state.HasChangeLane);
        Assert.False(state.IsChangeLaneSample);
        Assert.True(state.HasNavigation);
        Assert.Equal(4, state.NavigationLaneCount);
    }

    [Fact]
    public void Closer_sample_replaces_selected_bus_and_preserves_aggregate_count()
    {
        BusApproachSampleSelectionState state = BusApproachSampleSelectionPolicy.RecordSample(
            default,
            new BusApproachSampleSelectionInput(
                curvePosition: 0.30f,
                isBusOnlyLane: false,
                hasChangeLane: true,
                isChangeLaneSample: true,
                hasNavigation: true,
                navigationLaneCount: 2));

        state = BusApproachSampleSelectionPolicy.RecordSample(
            state,
            new BusApproachSampleSelectionInput(
                curvePosition: 0.90f,
                isBusOnlyLane: true,
                hasChangeLane: false,
                isChangeLaneSample: false,
                hasNavigation: false,
                navigationLaneCount: 0));

        Assert.Equal(0.90f, state.CurvePosition);
        Assert.Equal(2, state.HitCount);
        Assert.True(state.IsBusOnlyLane);
        Assert.False(state.HasChangeLane);
        Assert.False(state.IsChangeLaneSample);
        Assert.False(state.HasNavigation);
        Assert.Equal(0, state.NavigationLaneCount);
    }

    [Fact]
    public void Equal_curve_sample_replaces_selected_bus()
    {
        BusApproachSampleSelectionState state = BusApproachSampleSelectionPolicy.RecordSample(
            default,
            new BusApproachSampleSelectionInput(
                curvePosition: 0.50f,
                isBusOnlyLane: false,
                hasChangeLane: false,
                isChangeLaneSample: false,
                hasNavigation: false,
                navigationLaneCount: 0));

        state = BusApproachSampleSelectionPolicy.RecordSample(
            state,
            new BusApproachSampleSelectionInput(
                curvePosition: 0.50f,
                isBusOnlyLane: true,
                hasChangeLane: true,
                isChangeLaneSample: true,
                hasNavigation: true,
                navigationLaneCount: 5));

        Assert.Equal(0.50f, state.CurvePosition);
        Assert.Equal(2, state.HitCount);
        Assert.True(state.IsBusOnlyLane);
        Assert.True(state.HasChangeLane);
        Assert.True(state.IsChangeLaneSample);
        Assert.True(state.HasNavigation);
        Assert.Equal(5, state.NavigationLaneCount);
    }

    [Fact]
    public void Hit_count_saturates_at_byte_max()
    {
        BusApproachSampleSelectionState state = new(
            hasSample: true,
            curvePosition: 0.50f,
            hitCount: byte.MaxValue,
            isBusOnlyLane: false,
            hasChangeLane: false,
            isChangeLaneSample: false,
            hasNavigation: false,
            navigationLaneCount: 0);

        state = BusApproachSampleSelectionPolicy.RecordSample(
            state,
            new BusApproachSampleSelectionInput(
                curvePosition: 0.10f,
                isBusOnlyLane: false,
                hasChangeLane: false,
                isChangeLaneSample: false,
                hasNavigation: false,
                navigationLaneCount: 0));

        Assert.Equal(byte.MaxValue, state.HitCount);
    }

    [Fact]
    public void Navigation_lane_count_saturates_at_byte_max()
    {
        byte count = BusApproachSampleSelectionPolicy.ToByteCount(300);

        Assert.Equal(byte.MaxValue, count);
    }
}
