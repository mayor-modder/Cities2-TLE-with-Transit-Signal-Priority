namespace TrafficLightsEnhancement.Logic.Tsp;

public static class TspDecisionEngine
{
    public static TspRequest CombineRequests(IEnumerable<TspRequest> requests)
    {
        TspRequest best = default;

        foreach (TspRequest request in requests)
        {
            if (request.Strength > best.Strength)
            {
                best = request;
            }
        }

        return best;
    }

    public static TspDecision SelectNextPhase(IReadOnlyList<PhaseScore> phases, int currentPhaseIndex, TspRequest request)
    {
        if (request.ExtensionEligible && currentPhaseIndex >= 0 && currentPhaseIndex < phases.Count)
        {
            PhaseScore currentPhase = phases[currentPhaseIndex];
            bool currentServesRequest =
                (request.Source == TspSource.Track && currentPhase.ServesTrack) ||
                (request.Source == TspSource.PublicCar && currentPhase.ServesPublicCar);

            if (currentServesRequest)
            {
                return new TspDecision(currentPhaseIndex, canExtendCurrent: true);
            }
        }

        int bestPhaseIndex = currentPhaseIndex;
        float bestScore = float.MinValue;

        foreach (PhaseScore phase in phases)
        {
            float score = phase.WeightedWaiting;

            if (request.Source == TspSource.Track && phase.ServesTrack)
            {
                score += 1000f * request.Strength;
            }

            if (request.Source == TspSource.PublicCar && phase.ServesPublicCar)
            {
                score += 1000f * request.Strength;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestPhaseIndex = phase.PhaseIndex;
            }
        }

        bool canExtendCurrent = request.ExtensionEligible && bestPhaseIndex == currentPhaseIndex;
        return new TspDecision(bestPhaseIndex, canExtendCurrent);
    }
}
