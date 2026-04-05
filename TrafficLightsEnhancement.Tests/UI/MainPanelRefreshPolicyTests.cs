using TrafficLightsEnhancement.Logic.UI;
using Xunit;

namespace TrafficLightsEnhancement.Tests.UI;

public class MainPanelRefreshPolicyTests
{
    [Theory]
    [InlineData(MainPanelRefreshState.Hidden, false)]
    [InlineData(MainPanelRefreshState.Empty, false)]
    [InlineData(MainPanelRefreshState.Main, true)]
    [InlineData(MainPanelRefreshState.CustomPhase, true)]
    [InlineData(MainPanelRefreshState.TrafficGroups, true)]
    public void Open_tle_panel_states_refresh_on_simulation_ticks(MainPanelRefreshState state, bool expected)
    {
        Assert.Equal(expected, MainPanelRefreshPolicy.ShouldRefreshOnSimulationTick(state));
    }
}
