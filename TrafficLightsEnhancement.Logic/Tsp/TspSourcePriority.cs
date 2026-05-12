namespace TrafficLightsEnhancement.Logic.Tsp;

public static class TspSourcePriority
{
    public static int GetPriority(TspSource source)
    {
        return source switch
        {
            TspSource.Track => 2,
            TspSource.PublicCar => 1,
            _ => 0,
        };
    }

    public static bool IsPreferredRequest(TspRequest candidateRequest, TspRequest existingRequest)
    {
        int candidatePriority = GetPriority(candidateRequest.Source);
        int existingPriority = GetPriority(existingRequest.Source);

        return candidatePriority > existingPriority
            || (candidatePriority == existingPriority && candidateRequest.Strength > existingRequest.Strength);
    }
}
