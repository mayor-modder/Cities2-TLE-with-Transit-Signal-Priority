using TrafficLightsEnhancement.Logic.Tsp;
using Xunit;

namespace TrafficLightsEnhancement.Tests.Tsp;

public class TspVehicleFairnessPolicyTests
{
    [Fact]
    public void Tram_override_of_due_vehicle_group_records_pending_group()
    {
        var state = TspVehicleFairnessState.None;

        state = TspVehicleFairnessPolicy.UpdateAfterSelection(
            state,
            signalGroupCount: 8,
            currentSignalGroup: 3,
            baseSignalGroup: 4,
            selectedSignalGroup: 1,
            tspOverrideApplied: true);

        Assert.True(state.HasPendingVehiclePhase);
        Assert.Equal(4, state.PendingVehicleSignalGroup);
    }

    [Fact]
    public void Pending_vehicle_group_defers_conflicting_tsp_when_due_again()
    {
        var state = new TspVehicleFairnessState(pendingVehicleSignalGroup: 4);

        Assert.True(TspVehicleFairnessPolicy.ShouldDeferToPendingVehiclePhase(
            state,
            signalGroupCount: 8,
            currentSignalGroup: 3,
            baseSignalGroup: 4,
            requestedSignalGroup: 1,
            inFlightSignalGroup: 0));
    }

    [Fact]
    public void Pending_vehicle_group_does_not_defer_until_due_again()
    {
        var state = new TspVehicleFairnessState(pendingVehicleSignalGroup: 4);

        Assert.False(TspVehicleFairnessPolicy.ShouldDeferToPendingVehiclePhase(
            state,
            signalGroupCount: 8,
            currentSignalGroup: 3,
            baseSignalGroup: 2,
            requestedSignalGroup: 1,
            inFlightSignalGroup: 0));
    }

    [Fact]
    public void Running_pending_vehicle_group_clears_fairness_state()
    {
        var state = new TspVehicleFairnessState(pendingVehicleSignalGroup: 4);

        state = TspVehicleFairnessPolicy.Refresh(
            state,
            signalGroupCount: 8,
            currentSignalGroup: 4);

        Assert.False(state.HasPendingVehiclePhase);
    }
}
