using TrafficLightsEnhancement.Logic.Tsp;
using Xunit;

namespace TrafficLightsEnhancement.Tests.Tsp;

public class BusLaneRequestValidationTests
{
    [Fact]
    public void Disabled_tsp_blocks_validated_bus_requests()
    {
        bool built = TransitSignalPriorityRuntime.TryBuildRequestForLane(
            settings: new TransitSignalPrioritySettings(),
            isTrackLane: false,
            isPublicCarLane: true,
            hasValidatedBusOccupant: true,
            out _);

        Assert.False(built);
    }
}
