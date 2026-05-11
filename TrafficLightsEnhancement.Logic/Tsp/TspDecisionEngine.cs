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
            if (request.Source == TspSource.None)
            {
                continue;
            }

            if (!best.HasValue
                || GetSourcePriority(request.Source) > GetSourcePriority(best.Value.Source)
                || (GetSourcePriority(request.Source) == GetSourcePriority(best.Value.Source)
                    && request.Strength > best.Value.Strength))
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
                PhaseServesRequest(currentPhase, request.Source);

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

            if (PhaseServesRequest(phase, request.Source))
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

    private static bool PhaseServesRequest(PhaseScore phase, TspSource source)
    {
        return source switch
        {
            TspSource.Track => phase.ServesTrack,
            TspSource.PublicCar => phase.ServesPublicCar,
            _ => false,
        };
    }

    private static int GetSourcePriority(TspSource source)
    {
        return source switch
        {
            TspSource.Track => 2,
            TspSource.PublicCar => 1,
            _ => 0,
        };
    }
}
