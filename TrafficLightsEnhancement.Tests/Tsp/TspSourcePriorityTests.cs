using TrafficLightsEnhancement.Logic.Tsp;
using Xunit;

namespace TrafficLightsEnhancement.Tests.Tsp;

public class TspSourcePriorityTests
{
    [Theory]
    [InlineData(TspSource.Track, 2)]
    [InlineData(TspSource.PublicCar, 1)]
    [InlineData(TspSource.None, 0)]
    public void Get_priority_orders_transit_sources(TspSource source, int expected)
    {
        Assert.Equal(expected, TspSourcePriority.GetPriority(source));
    }

    [Fact]
    public void Track_request_outranks_stronger_public_car_request()
    {
        var track = new TspRequest(TspSource.Track, strength: 0.5f, extensionEligible: false);
        var bus = new TspRequest(TspSource.PublicCar, strength: 1f, extensionEligible: false);

        Assert.True(TspSourcePriority.IsPreferredRequest(track, bus));
        Assert.False(TspSourcePriority.IsPreferredRequest(bus, track));
    }

    [Fact]
    public void Track_tie_outranks_public_car_tie()
    {
        var track = new TspRequest(TspSource.Track, strength: 1f, extensionEligible: false);
        var bus = new TspRequest(TspSource.PublicCar, strength: 1f, extensionEligible: false);

        Assert.True(TspSourcePriority.IsPreferredRequest(track, bus));
        Assert.False(TspSourcePriority.IsPreferredRequest(bus, track));
    }
}
