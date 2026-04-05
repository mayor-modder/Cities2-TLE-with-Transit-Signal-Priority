using TrafficLightsEnhancement.Logic.Tsp;
using Xunit;

namespace TrafficLightsEnhancement.Tests.Tsp;

public class TspEarlyDetectionTests
{
    [Fact]
    public void Approach_lane_resolution_prefers_source_lane_when_present()
    {
        int resolvedLane = EarlyApproachDetection.ResolveApproachLane(
            signaledLane: 10,
            sourceLane: 22,
            nullLane: 0);

        Assert.Equal(22, resolvedLane);
    }

    [Fact]
    public void Tram_detection_matches_immediate_upstream_lane()
    {
        bool matched = EarlyApproachDetection.IsEligibleTramApproachLane(
            currentLane: 10,
            approachLane: 20,
            upstreamLane: 10,
            nullLane: 0);

        Assert.True(matched);
    }

    [Fact]
    public void Moving_vehicle_without_stop_suppression_triggers_early_detection()
    {
        bool triggered = EarlyApproachDetection.IsMovingEligibleApproachState(
            isEligibleLane: true,
            isVehicleMoving: true,
            suppressionFlags: TransitApproachSuppressionFlags.None);

        Assert.True(triggered);
    }

    [Fact]
    public void Scan_wide_selection_falls_back_to_petitioner_when_early_request_is_absent()
    {
        TspRequest petitioner = new(TspSource.Track, strength: 0.8f, extensionEligible: true);
        TransitApproachScanState scanState = default;

        scanState = EarlyApproachDetection.RecordLaneRequests(
            scanState,
            earlyRequest: null,
            petitionerRequest: petitioner);

        TspRequest? selected = EarlyApproachDetection.PreferEarlyRequest(
            scanState.EarlyRequest,
            scanState.PetitionerRequest);

        Assert.True(selected.HasValue);
        Assert.Equal(petitioner.Source, selected.Value.Source);
        Assert.Equal(petitioner.Strength, selected.Value.Strength);
        Assert.Equal(petitioner.ExtensionEligible, selected.Value.ExtensionEligible);
    }

    [Fact]
    public void Scan_wide_selection_prefers_later_early_request_over_earlier_petitioner()
    {
        TspRequest earlyRequest = new(TspSource.PublicCar, strength: 1f, extensionEligible: false);
        TspRequest petitionerRequest = new(TspSource.Track, strength: 0.25f, extensionEligible: true);
        TransitApproachScanState scanState = default;

        scanState = EarlyApproachDetection.RecordLaneRequests(
            scanState,
            earlyRequest: null,
            petitionerRequest: petitionerRequest);
        scanState = EarlyApproachDetection.RecordLaneRequests(
            scanState,
            earlyRequest: earlyRequest,
            petitionerRequest: null);

        TspRequest? selected = EarlyApproachDetection.PreferEarlyRequest(
            scanState.EarlyRequest,
            scanState.PetitionerRequest);

        Assert.True(selected.HasValue);
        Assert.Equal(earlyRequest.Source, selected.Value.Source);
        Assert.Equal(earlyRequest.Strength, selected.Value.Strength);
        Assert.Equal(earlyRequest.ExtensionEligible, selected.Value.ExtensionEligible);
    }
}
