using C2VM.TrafficLightsEnhancement.Components;
using TrafficLightsEnhancement.Logic.Tsp;
using Xunit;
using EcsTspRuntime = C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystems.Simulation.TransitSignalPriorityRuntime;

namespace TrafficLightsEnhancement.Ecs.Tests;

public class TransitSignalPriorityRuntimeSelectionTests
{
    [Theory]
    [InlineData(BusPrioritySuppressionReason.None, TransitSignalPriorityBusDecision.None)]
    [InlineData(BusPrioritySuppressionReason.Boarding, TransitSignalPriorityBusDecision.SuppressedBoarding)]
    [InlineData(BusPrioritySuppressionReason.NearSideStop, TransitSignalPriorityBusDecision.SuppressedNearSideStop)]
    [InlineData(BusPrioritySuppressionReason.UnknownStopRelation, TransitSignalPriorityBusDecision.SuppressedUnknownStopRelation)]
    public void Bus_suppression_reason_maps_to_specific_diagnostic_decision(
        BusPrioritySuppressionReason reason,
        TransitSignalPriorityBusDecision expected)
    {
        Assert.Equal(expected, EcsTspRuntime.MapBusSuppressionReasonToDecision(reason));
    }

    [Fact]
    public void Preferred_request_role_stays_with_winning_track_request()
    {
        var trackRequest = new TspRequest(TspSource.Track, strength: 0.5f, extensionEligible: false);
        var busRequest = new TspRequest(TspSource.PublicCar, strength: 1f, extensionEligible: false);

        TspRequest? selected = EcsTspRuntime.SelectPreferredRequestAndRole(
            trackRequest,
            TransitSignalPriorityApproachLaneRole.UpstreamLane,
            busRequest,
            TransitSignalPriorityApproachLaneRole.ApproachLane,
            out TransitSignalPriorityApproachLaneRole selectedRole);

        Assert.True(selected.HasValue);
        Assert.Equal(TspSource.Track, selected.Value.Source);
        Assert.Equal(TransitSignalPriorityApproachLaneRole.UpstreamLane, selectedRole);
    }

    [Fact]
    public void Preferred_request_role_uses_bus_role_when_bus_replaces_empty_candidate()
    {
        var busRequest = new TspRequest(TspSource.PublicCar, strength: 1f, extensionEligible: false);

        TspRequest? selected = EcsTspRuntime.SelectPreferredRequestAndRole(
            existingRequest: null,
            existingRole: TransitSignalPriorityApproachLaneRole.None,
            candidateRequest: busRequest,
            candidateRole: TransitSignalPriorityApproachLaneRole.ApproachLane,
            out TransitSignalPriorityApproachLaneRole selectedRole);

        Assert.True(selected.HasValue);
        Assert.Equal(TspSource.PublicCar, selected.Value.Source);
        Assert.Equal(TransitSignalPriorityApproachLaneRole.ApproachLane, selectedRole);
    }
}
