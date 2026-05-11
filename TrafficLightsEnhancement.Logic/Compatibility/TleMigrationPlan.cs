using System;
using System.Collections.Generic;

namespace TrafficLightsEnhancement.Logic.Compatibility;

public enum TleMigrationStep
{
    SignalDelayData,
    TrafficGroupMembers,
    CustomTrafficLights
}

public static class TleMigrationPlan
{
    private const int Version1 = 1;
    private const int Version2 = 2;
    private const int Version5 = 5;

    private static readonly TleMigrationStep[] FromBeforeV1 =
    {
        TleMigrationStep.SignalDelayData,
        TleMigrationStep.TrafficGroupMembers,
        TleMigrationStep.CustomTrafficLights
    };

    private static readonly TleMigrationStep[] FromV1 =
    {
        TleMigrationStep.TrafficGroupMembers,
        TleMigrationStep.CustomTrafficLights
    };

    private static readonly TleMigrationStep[] FromV2ToV4 =
    {
        TleMigrationStep.CustomTrafficLights
    };

    public static IReadOnlyList<TleMigrationStep> GetSteps(int loadedVersion)
    {
        if (loadedVersion < Version1)
        {
            return FromBeforeV1;
        }

        if (loadedVersion < Version2)
        {
            return FromV1;
        }

        if (loadedVersion < Version5)
        {
            return FromV2ToV4;
        }

        return Array.Empty<TleMigrationStep>();
    }
}
