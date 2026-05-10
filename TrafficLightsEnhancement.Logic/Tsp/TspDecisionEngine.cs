using System;
using System.Collections.Generic;

namespace TrafficLightsEnhancement.Logic.Tsp;

public static class TspDecisionEngine
{
    public static TspRequest? CombineRequests(IEnumerable<TspRequest> requests)
    {
        if (requests is null)
        {
            throw new ArgumentNullException(nameof(requests));
        }

        TspRequest? best = null;

        foreach (TspRequest request in requests)
        {
            if (request.Source == TspSource.Track && (!best.HasValue || request.Strength > best.Value.Strength))
            {
                best = request;
            }
        }

        return best;
    }

    public static TspDecision SelectNextPhase(IReadOnlyList<PhaseScore> phases, int currentPhaseIndex, TspRequest request)
    {
        if (phases is null)
        {
            throw new ArgumentNullException(nameof(phases));
        }

        if (phases.Count == 0)
        {
            return new TspDecision(-1, canExtendCurrent: false);
        }

        if (request.ExtensionEligible && currentPhaseIndex >= 0 && currentPhaseIndex < phases.Count)
        {
            PhaseScore currentPhase = phases[currentPhaseIndex];
            bool currentServesRequest =
                request.Source == TspSource.Track && currentPhase.ServesTrack;

            if (currentServesRequest)
            {
                return new TspDecision(currentPhaseIndex, canExtendCurrent: true);
            }
        }

        int bestPhaseIndex = -1;
        float bestScore = float.MinValue;

        foreach (PhaseScore phase in phases)
        {
            float score = phase.WeightedWaiting;

            if (request.Source == TspSource.Track && phase.ServesTrack)
            {
                score += 1000f * request.Strength;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestPhaseIndex = phase.PhaseIndex;
            }
        }

        return new TspDecision(bestPhaseIndex, canExtendCurrent: false);
    }
}
