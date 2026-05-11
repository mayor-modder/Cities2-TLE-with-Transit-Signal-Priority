using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using NetCarLane = Game.Net.CarLane;
using VehicleCarLaneFlags = Game.Vehicles.CarLaneFlags;

namespace C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystems.Simulation;

public struct BusApproachSample
{
    public Entity VehicleEntity;
    public Entity LaneEntity;
    public float CurvePosition;
    public float ChangeProgress;
    public float Speed;
    public byte HitCount;
    public byte IsBusOnlyLane;
    public byte HasChangeLane;
    public byte IsChangeLaneSample;
    public byte HasNavigation;
    public byte NavigationLaneCount;
    public PublicTransportFlags PublicTransportState;
    public VehicleCarLaneFlags VehicleLaneFlags;
}

internal static class BusApproachIndex
{
    public static NativeParallelHashMap<Entity, BusApproachSample> Build(
        EntityQuery busTransitQuery,
        ExtraTypeHandle extraTypeHandle,
        Allocator allocator)
    {
        using NativeArray<Entity> busEntities = busTransitQuery.ToEntityArray(Allocator.Temp);
        int capacity = math.max(1, busEntities.Length * 2);
        var index = new NativeParallelHashMap<Entity, BusApproachSample>(capacity, allocator);

        for (int i = 0; i < busEntities.Length; i++)
        {
            Entity vehicleEntity = busEntities[i];
            if (!extraTypeHandle.m_PublicTransport.TryGetComponent(vehicleEntity, out var publicTransport)
                || !extraTypeHandle.m_CarCurrentLane.TryGetComponent(vehicleEntity, out var carCurrentLane)
                || !extraTypeHandle.m_PrefabRef.TryGetComponent(vehicleEntity, out var prefabRef)
                || !extraTypeHandle.m_PublicTransportVehicleData.TryGetComponent(prefabRef.m_Prefab, out var vehicleData)
                || vehicleData.m_TransportType != TransportType.Bus)
            {
                continue;
            }

            float speed = extraTypeHandle.m_Moving.TryGetComponent(vehicleEntity, out Moving moving)
                ? math.length(moving.m_Velocity)
                : 0f;
            bool hasChangeLane = carCurrentLane.m_ChangeLane != Entity.Null;
            bool hasNavigation = extraTypeHandle.m_CarNavigation.HasComponent(vehicleEntity);
            byte navigationLaneCount = extraTypeHandle.m_CarNavigationLane.TryGetBuffer(vehicleEntity, out var navigationLanes)
                ? ToByteCount(navigationLanes.Length)
                : (byte)0;

            TryRecordLaneSample(
                index,
                vehicleEntity,
                carCurrentLane.m_Lane,
                carCurrentLane.m_CurvePosition.x,
                carCurrentLane.m_ChangeProgress,
                speed,
                hasChangeLane,
                isChangeLaneSample: false,
                hasNavigation,
                navigationLaneCount,
                publicTransport.m_State,
                carCurrentLane.m_LaneFlags,
                extraTypeHandle);

            if (hasChangeLane)
            {
                TryRecordLaneSample(
                    index,
                    vehicleEntity,
                    carCurrentLane.m_ChangeLane,
                    carCurrentLane.m_CurvePosition.x,
                    carCurrentLane.m_ChangeProgress,
                    speed,
                    hasChangeLane,
                    isChangeLaneSample: true,
                    hasNavigation,
                    navigationLaneCount,
                    publicTransport.m_State,
                    carCurrentLane.m_LaneFlags,
                    extraTypeHandle);
            }
        }

        return index;
    }

    private static void TryRecordLaneSample(
        NativeParallelHashMap<Entity, BusApproachSample> index,
        Entity vehicleEntity,
        Entity laneEntity,
        float curvePosition,
        float changeProgress,
        float speed,
        bool hasChangeLane,
        bool isChangeLaneSample,
        bool hasNavigation,
        byte navigationLaneCount,
        PublicTransportFlags publicTransportState,
        VehicleCarLaneFlags vehicleLaneFlags,
        ExtraTypeHandle extraTypeHandle)
    {
        if (laneEntity == Entity.Null || !extraTypeHandle.m_CarLane.HasComponent(laneEntity))
        {
            return;
        }

        BusApproachSample sample = new()
        {
            VehicleEntity = vehicleEntity,
            LaneEntity = laneEntity,
            CurvePosition = curvePosition,
            ChangeProgress = changeProgress,
            Speed = speed,
            HitCount = 1,
            IsBusOnlyLane = IsBusOnlyLane(extraTypeHandle, laneEntity) ? (byte)1 : (byte)0,
            HasChangeLane = hasChangeLane ? (byte)1 : (byte)0,
            IsChangeLaneSample = isChangeLaneSample ? (byte)1 : (byte)0,
            HasNavigation = hasNavigation ? (byte)1 : (byte)0,
            NavigationLaneCount = navigationLaneCount,
            PublicTransportState = publicTransportState,
            VehicleLaneFlags = vehicleLaneFlags,
        };

        if (!index.TryGetValue(laneEntity, out BusApproachSample existing))
        {
            index[laneEntity] = sample;
            return;
        }

        sample.HitCount = existing.HitCount == byte.MaxValue ? byte.MaxValue : (byte)(existing.HitCount + 1);
        if (existing.CurvePosition > curvePosition)
        {
            existing.HitCount = sample.HitCount;
            existing.HasChangeLane = existing.HasChangeLane != 0 || hasChangeLane ? (byte)1 : (byte)0;
            existing.HasNavigation = existing.HasNavigation != 0 || hasNavigation ? (byte)1 : (byte)0;
            existing.NavigationLaneCount = existing.NavigationLaneCount > navigationLaneCount ? existing.NavigationLaneCount : navigationLaneCount;
            index[laneEntity] = existing;
            return;
        }

        index[laneEntity] = sample;
    }

    private static bool IsBusOnlyLane(ExtraTypeHandle extraTypeHandle, Entity laneEntity)
    {
        return extraTypeHandle.m_CarLane.TryGetComponent(laneEntity, out NetCarLane carLane)
            && (carLane.m_Flags & Game.Net.CarLaneFlags.PublicOnly) != 0;
    }

    private static byte ToByteCount(int value)
    {
        return value >= byte.MaxValue ? byte.MaxValue : (byte)value;
    }
}
