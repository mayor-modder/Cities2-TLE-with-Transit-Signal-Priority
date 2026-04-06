using TrafficLightsEnhancement.Logic.Tsp;
using Xunit;

namespace TrafficLightsEnhancement.Tests.Tsp;

public class TransitApproachEntityResolverTests
{
    [Fact]
    public void Prefers_lane_object_when_it_already_has_transit_runtime()
    {
        int selected = TransitApproachEntityResolver.SelectPreferredTransitEntity(
            laneObjectEntity: 11,
            laneObjectHasTransitRuntime: true,
            ownerEntity: 22,
            ownerHasTransitRuntime: true,
            grandOwnerEntity: 33,
            grandOwnerHasTransitRuntime: true,
            nullEntity: 0);

        Assert.Equal(11, selected);
    }

    [Fact]
    public void Falls_back_to_owner_when_lane_object_is_only_a_child_entity()
    {
        int selected = TransitApproachEntityResolver.SelectPreferredTransitEntity(
            laneObjectEntity: 11,
            laneObjectHasTransitRuntime: false,
            ownerEntity: 22,
            ownerHasTransitRuntime: true,
            grandOwnerEntity: 33,
            grandOwnerHasTransitRuntime: false,
            nullEntity: 0);

        Assert.Equal(22, selected);
    }

    [Fact]
    public void Falls_back_to_grand_owner_when_needed()
    {
        int selected = TransitApproachEntityResolver.SelectPreferredTransitEntity(
            laneObjectEntity: 11,
            laneObjectHasTransitRuntime: false,
            ownerEntity: 22,
            ownerHasTransitRuntime: false,
            grandOwnerEntity: 33,
            grandOwnerHasTransitRuntime: true,
            nullEntity: 0);

        Assert.Equal(33, selected);
    }

    [Fact]
    public void Returns_null_entity_when_no_transit_runtime_entity_exists()
    {
        int selected = TransitApproachEntityResolver.SelectPreferredTransitEntity(
            laneObjectEntity: 11,
            laneObjectHasTransitRuntime: false,
            ownerEntity: 22,
            ownerHasTransitRuntime: false,
            grandOwnerEntity: 33,
            grandOwnerHasTransitRuntime: false,
            nullEntity: 0);

        Assert.Equal(0, selected);
    }
}
