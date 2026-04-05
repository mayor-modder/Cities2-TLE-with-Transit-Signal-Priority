namespace TrafficLightsEnhancement.Logic.UI;

public enum MainPanelRefreshState : byte
{
    Hidden = 0,
    Empty = 1,
    Main = 2,
    CustomPhase = 3,
    TrafficGroups = 4,
}

public static class MainPanelRefreshPolicy
{
    public static bool ShouldRefreshOnSimulationTick(MainPanelRefreshState state)
    {
        return state is MainPanelRefreshState.Main
            or MainPanelRefreshState.CustomPhase
            or MainPanelRefreshState.TrafficGroups;
    }
}
