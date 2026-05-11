using System;
using System.Globalization;
using C2VM.TrafficLightsEnhancement.Components;

namespace C2VM.TrafficLightsEnhancement.Utils;

public static class CustomPhaseDataUpdate
{
    public static bool TryApply(string key, object value, ref CustomPhaseData phase)
    {
        switch (key)
        {
            case "MinimumDuration":
                phase.m_MinimumDuration = Convert.ToUInt16(value, CultureInfo.InvariantCulture);
                if (phase.m_MinimumDuration > phase.m_MaximumDuration)
                {
                    phase.m_MaximumDuration = phase.m_MinimumDuration;
                }
                return true;

            case "MaximumDuration":
                phase.m_MaximumDuration = Convert.ToUInt16(value, CultureInfo.InvariantCulture);
                if (phase.m_MinimumDuration > phase.m_MaximumDuration)
                {
                    phase.m_MinimumDuration = phase.m_MaximumDuration;
                }
                return true;

            case "TargetDurationMultiplier":
                phase.m_TargetDurationMultiplier = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                return true;

            case "IntervalExponent":
                phase.m_IntervalExponent = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                return true;

            case "LinkedWithNextPhase":
                phase.m_Options ^= CustomPhaseData.Options.LinkedWithNextPhase;
                return true;

            case "EndPhasePrematurely":
                phase.m_Options ^= CustomPhaseData.Options.EndPhasePrematurely;
                return true;

            case "carOpenDelay":
                phase.m_CarOpenDelay = Convert.ToInt16(value, CultureInfo.InvariantCulture);
                return true;

            case "carCloseDelay":
                phase.m_CarCloseDelay = Convert.ToInt16(value, CultureInfo.InvariantCulture);
                return true;

            case "publicCarOpenDelay":
                phase.m_PublicCarOpenDelay = Convert.ToInt16(value, CultureInfo.InvariantCulture);
                return true;

            case "publicCarCloseDelay":
                phase.m_PublicCarCloseDelay = Convert.ToInt16(value, CultureInfo.InvariantCulture);
                return true;

            case "trackOpenDelay":
                phase.m_TrackOpenDelay = Convert.ToInt16(value, CultureInfo.InvariantCulture);
                return true;

            case "trackCloseDelay":
                phase.m_TrackCloseDelay = Convert.ToInt16(value, CultureInfo.InvariantCulture);
                return true;

            case "pedestrianOpenDelay":
                phase.m_PedestrianOpenDelay = Convert.ToInt16(value, CultureInfo.InvariantCulture);
                return true;

            case "pedestrianCloseDelay":
                phase.m_PedestrianCloseDelay = Convert.ToInt16(value, CultureInfo.InvariantCulture);
                return true;

            case "bicycleOpenDelay":
                phase.m_BicycleOpenDelay = Convert.ToInt16(value, CultureInfo.InvariantCulture);
                return true;

            case "bicycleCloseDelay":
                phase.m_BicycleCloseDelay = Convert.ToInt16(value, CultureInfo.InvariantCulture);
                return true;

            case "ChangeMetric":
                phase.m_ChangeMetric = (CustomPhaseData.StepChangeMetric)Convert.ToInt32(value, CultureInfo.InvariantCulture);
                return true;

            case "WaitFlowBalance":
                phase.m_WaitFlowBalance = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                return true;

            case "CarWeight":
                phase.m_CarWeight = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                return true;

            case "PublicCarWeight":
                phase.m_PublicCarWeight = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                return true;

            case "TrackWeight":
                phase.m_TrackWeight = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                return true;

            case "PedestrianWeight":
                phase.m_PedestrianWeight = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                return true;

            case "BicycleWeight":
                phase.m_BicycleWeight = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                return true;

            case "SmoothingFactor":
                phase.m_SmoothingFactor = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                return true;

            default:
                return false;
        }
    }
}
