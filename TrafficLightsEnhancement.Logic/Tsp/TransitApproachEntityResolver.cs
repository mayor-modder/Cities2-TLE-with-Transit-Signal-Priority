namespace TrafficLightsEnhancement.Logic.Tsp;

public static class TransitApproachEntityResolver
{
    public static TEntity SelectPreferredTransitEntity<TEntity>(
        TEntity laneObjectEntity,
        bool laneObjectHasTransitRuntime,
        TEntity ownerEntity,
        bool ownerHasTransitRuntime,
        TEntity grandOwnerEntity,
        bool grandOwnerHasTransitRuntime,
        TEntity nullEntity)
        where TEntity : struct, IEquatable<TEntity>
    {
        if (laneObjectHasTransitRuntime)
        {
            return laneObjectEntity;
        }

        if (ownerHasTransitRuntime)
        {
            return ownerEntity;
        }

        if (grandOwnerHasTransitRuntime)
        {
            return grandOwnerEntity;
        }

        return nullEntity;
    }
}
