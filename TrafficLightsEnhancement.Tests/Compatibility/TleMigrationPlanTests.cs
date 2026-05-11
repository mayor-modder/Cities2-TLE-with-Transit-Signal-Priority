using TrafficLightsEnhancement.Logic.Compatibility;
using Xunit;

namespace TrafficLightsEnhancement.Tests.Compatibility;

public class TleMigrationPlanTests
{
    [Theory]
    [InlineData(0, TleMigrationStep.SignalDelayData, TleMigrationStep.TrafficGroupMembers, TleMigrationStep.CustomTrafficLights)]
    [InlineData(1, TleMigrationStep.TrafficGroupMembers, TleMigrationStep.CustomTrafficLights)]
    [InlineData(2, TleMigrationStep.CustomTrafficLights)]
    [InlineData(3, TleMigrationStep.CustomTrafficLights)]
    [InlineData(4, TleMigrationStep.CustomTrafficLights)]
    public void Older_versions_run_each_required_migration_once(int loadedVersion, params TleMigrationStep[] expectedSteps)
    {
        var plan = TleMigrationPlan.GetSteps(loadedVersion);

        Assert.Equal(expectedSteps, plan);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    public void Current_or_newer_versions_run_no_versioned_migrations(int loadedVersion)
    {
        var plan = TleMigrationPlan.GetSteps(loadedVersion);

        Assert.Empty(plan);
    }

    [Fact]
    public void Negative_versions_conservatively_run_all_known_migrations()
    {
        var plan = TleMigrationPlan.GetSteps(-1);

        Assert.Equal(
            [
                TleMigrationStep.SignalDelayData,
                TleMigrationStep.TrafficGroupMembers,
                TleMigrationStep.CustomTrafficLights
            ],
            plan);
    }
}
