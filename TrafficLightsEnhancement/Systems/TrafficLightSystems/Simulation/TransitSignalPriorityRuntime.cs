using C2VM.TrafficLightsEnhancement.Components;
using Game.Net;
using Game.Prefabs;
using Game.Vehicles;
using TrafficLightsEnhancement.Logic.Tsp;
using Unity.Entities;
using NetCarLaneFlags = Game.Net.CarLaneFlags;
using NetSubLane = Game.Net.SubLane;
using VehicleCarLaneFlags = Game.Vehicles.CarLaneFlags;

namespace C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystems.Simulation;

public static class TransitSignalPriorityRuntime
{
    private struct TransitApproachCandidate
    {
        public TspRequest Request;
        public LaneSignal LaneSignal;
        public byte TargetSignalGroup;
    }

    private const float MovingTrainSpeedThreshold = 0.5f;
    private const float BusApproachCurveThreshold = 0.35f;
    private const VehicleCarLaneFlags BusStoppedLaneFlags =
        VehicleCarLaneFlags.IsBlocked
        | VehicleCarLaneFlags.EndReached;

    public static bool TryResolveActiveLocalRequest(
        PatchedTrafficLightSystem.UpdateTrafficLightsJob job,
        Entity junctionEntity,
        DynamicBuffer<NetSubLane> subLanes,
        TrafficLights trafficLights,
        out TransitSignalPriorityRequest request,
        out Components.TransitSignalPrioritySettings settings)
    {
        request = default;
        settings = default;

        if (!job.m_ExtraTypeHandle.m_TransitSignalPrioritySettingsLookup.TryGetComponent(junctionEntity, out settings) || !settings.m_Enabled)
        {
            return false;
        }

        var availability = TspPolicy.GetAvailability(
            new global::TrafficLightsEnhancement.Logic.Tsp.TransitSignalPrioritySettings
            {
                m_Enabled = settings.m_Enabled,
                m_AllowTrackRequests = settings.m_AllowTrackRequests,
                m_AllowPublicCarRequests = settings.m_AllowPublicCarRequests,
                m_AllowGroupPropagation = settings.m_AllowGroupPropagation,
                m_RequestHorizonTicks = settings.m_RequestHorizonTicks,
                m_MaxGreenExtensionTicks = settings.m_MaxGreenExtensionTicks,
            },
            isGroupedIntersection: false);

        if (!availability.IsRuntimeEligible)
        {
            return false;
        }

        TransitSignalPriorityRequest freshRequest = default;
        bool hasFreshRequest = TryBuildFreshRequest(job, subLanes, trafficLights, settings, out freshRequest);

        TransitSignalPriorityRequest priorRequest = default;
        bool hasExistingRequest = job.m_ExtraTypeHandle.m_TransitSignalPriorityRequest.TryGetComponent(junctionEntity, out priorRequest)
            && priorRequest.m_TargetSignalGroup > 0
            && priorRequest.m_Strength > 0f;

        TspSignalRequest? freshSignalRequest = hasFreshRequest ? ToSignalRequest(freshRequest) : null;
        TspSignalRequest? existingRequest = hasExistingRequest ? ToSignalRequest(priorRequest) : null;

        if (!TspPreemptionPolicy.TryRefreshOrLatchRequest(
                freshSignalRequest,
                existingRequest,
                settings.m_RequestHorizonTicks,
                trafficLights.m_CurrentSignalGroup,
                out var activeRequest))
        {
            return false;
        }

        request = FromSignalRequest(activeRequest);
        return true;
    }

    public static bool ShouldHoldCurrentGroup(
        TrafficLights trafficLights,
        TransitSignalPriorityRequest request,
        ushort maxGreenExtensionTicks)
    {
        return TspPreemptionPolicy.ShouldHoldCurrentGroup(
            trafficLights.m_CurrentSignalGroup,
            ToSignalRequest(request),
            trafficLights.m_Timer,
            maxGreenExtensionTicks);
    }

    public static int GetMinimumGreenDurationTicks(
        int defaultMinimumGreenTicks,
        TrafficLights trafficLights,
        TransitSignalPriorityRequest request)
    {
        return TspPreemptionPolicy.GetMinimumGreenDurationTicks(
            defaultMinimumGreenTicks,
            trafficLights.m_CurrentSignalGroup,
            ToSignalRequest(request));
    }

    private static bool TryBuildFreshRequest(
        PatchedTrafficLightSystem.UpdateTrafficLightsJob job,
        DynamicBuffer<NetSubLane> subLanes,
        TrafficLights trafficLights,
        Components.TransitSignalPrioritySettings settings,
        out TransitSignalPriorityRequest request)
    {
        request = default;

        var logicSettings = new global::TrafficLightsEnhancement.Logic.Tsp.TransitSignalPrioritySettings
        {
            m_Enabled = settings.m_Enabled,
            m_AllowTrackRequests = settings.m_AllowTrackRequests,
            m_AllowPublicCarRequests = settings.m_AllowPublicCarRequests,
            m_AllowGroupPropagation = settings.m_AllowGroupPropagation,
            m_RequestHorizonTicks = settings.m_RequestHorizonTicks,
            m_MaxGreenExtensionTicks = settings.m_MaxGreenExtensionTicks,
        };

        TransitApproachScanState scanState = default;
        TransitApproachCandidate? earlyCandidate = null;
        TransitApproachCandidate? petitionerCandidate = null;

        foreach (var subLane in subLanes)
        {
            Entity subLaneEntity = subLane.m_SubLane;
            if (!job.m_LaneSignalData.TryGetComponent(subLaneEntity, out var laneSignal))
            {
                continue;
            }

            Entity approachLaneEntity = ResolveApproachLane(job, subLaneEntity);
            bool isTramTrackLane = IsTramTrackLane(job, approachLaneEntity);
            bool isPublicCarLane = IsPublicOnlyCarLane(job, approachLaneEntity);

            if (!global::TrafficLightsEnhancement.Logic.Tsp.TransitSignalPriorityRuntime.TryBuildRequestForLane(
                    logicSettings,
                    isTramTrackLane,
                    isPublicCarLane,
                    out var logicRequest))
            {
                continue;
            }

            TspRequest? earlyRequest = null;
            if (TryBuildEarlyApproachRequestForLane(
                    job,
                    approachLaneEntity,
                    isTramTrackLane,
                    isPublicCarLane,
                    logicRequest,
                    out var detectedEarlyRequest))
            {
                earlyRequest = detectedEarlyRequest;
            }

            TspRequest? petitionerRequest = null;
            if (TryBuildPetitionerRequestForLane(laneSignal, logicRequest, out var detectedPetitionerRequest))
            {
                petitionerRequest = detectedPetitionerRequest;
            }

            if (!earlyRequest.HasValue && !petitionerRequest.HasValue)
            {
                continue;
            }

            byte currentSignalGroup = trafficLights.m_CurrentSignalGroup;
            byte targetSignalGroup = GetTargetSignalGroup(laneSignal.m_GroupMask, currentSignalGroup);
            if (targetSignalGroup == 0)
            {
                continue;
            }

            scanState = EarlyApproachDetection.RecordLaneRequests(scanState, earlyRequest, petitionerRequest);

            if (earlyRequest.HasValue && !earlyCandidate.HasValue)
            {
                earlyCandidate = new TransitApproachCandidate
                {
                    Request = earlyRequest.Value,
                    LaneSignal = laneSignal,
                    TargetSignalGroup = targetSignalGroup,
                };
            }

            if (petitionerRequest.HasValue && !petitionerCandidate.HasValue)
            {
                petitionerCandidate = new TransitApproachCandidate
                {
                    Request = petitionerRequest.Value,
                    LaneSignal = laneSignal,
                    TargetSignalGroup = targetSignalGroup,
                };
            }
        }

        TspRequest? selectedRequest = EarlyApproachDetection.PreferEarlyRequest(
            scanState.EarlyRequest,
            scanState.PetitionerRequest);

        if (!selectedRequest.HasValue)
        {
            return false;
        }

        TransitApproachCandidate? selectedCandidate = scanState.EarlyRequest.HasValue
            ? earlyCandidate
            : petitionerCandidate;

        if (!selectedCandidate.HasValue)
        {
            return false;
        }

        request = CreateRequest(
            selectedCandidate.Value.Request,
            settings.m_RequestHorizonTicks,
            trafficLights.m_CurrentSignalGroup,
            selectedCandidate.Value.TargetSignalGroup,
            selectedCandidate.Value.LaneSignal);
        return true;
    }

    private static Entity ResolveApproachLane(
        PatchedTrafficLightSystem.UpdateTrafficLightsJob job,
        Entity subLaneEntity)
    {
        Entity sourceSubLane = Entity.Null;
        if (job.m_ExtraTypeHandle.m_ExtraLaneSignal.TryGetComponent(subLaneEntity, out var extraLaneSignal))
        {
            sourceSubLane = extraLaneSignal.m_SourceSubLane;
        }

        return EarlyApproachDetection.ResolveApproachLane(subLaneEntity, sourceSubLane, Entity.Null);
    }

    private static bool TryBuildEarlyApproachRequestForLane(
        PatchedTrafficLightSystem.UpdateTrafficLightsJob job,
        Entity approachLaneEntity,
        bool isTramTrackLane,
        bool isPublicCarLane,
        TspRequest laneRequest,
        out TspRequest request)
    {
        request = default;

        if (isTramTrackLane)
        {
            return TryBuildEarlyApproachRequestForTrackLane(job, approachLaneEntity, laneRequest, out request);
        }

        if (!job.m_LaneObjects.TryGetBuffer(approachLaneEntity, out var laneObjects) || laneObjects.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < laneObjects.Length; i++)
        {
            if (TryGetMovingTransitApproachRequest(
                    job,
                    approachLaneEntity,
                    approachLaneEntity,
                    Entity.Null,
                    laneObjects[i],
                    isTramTrackLane: false,
                    isPublicCarLane,
                    laneRequest,
                    out request))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryBuildPetitionerRequestForLane(
        LaneSignal laneSignal,
        TspRequest laneRequest,
        out TspRequest request)
    {
        request = default;

        if (laneSignal.m_Petitioner == Entity.Null)
        {
            return false;
        }

        request = laneRequest;
        return true;
    }

    private static bool TryGetMovingTransitApproachRequest(
        PatchedTrafficLightSystem.UpdateTrafficLightsJob job,
        Entity scanLaneEntity,
        Entity approachLaneEntity,
        Entity upstreamLaneEntity,
        LaneObject laneObject,
        bool isTramTrackLane,
        bool isPublicCarLane,
        TspRequest laneRequest,
        out TspRequest request)
    {
        request = default;

        Entity vehicleEntity = laneObject.m_LaneObject;
        if (!job.m_ExtraTypeHandle.m_PublicTransport.TryGetComponent(vehicleEntity, out var publicTransport))
        {
            return false;
        }

        TransitApproachSuppressionFlags suppressionFlags = GetSuppressionFlags(publicTransport.m_State);
        bool isVehicleMoving = isTramTrackLane
            ? IsMovingTrackVehicle(job, vehicleEntity, approachLaneEntity, upstreamLaneEntity)
            : isPublicCarLane && IsMovingPublicTransportRoadVehicle(job, vehicleEntity, scanLaneEntity, laneObject, suppressionFlags);

        if (!EarlyApproachDetection.IsMovingEligibleApproachState(
                isEligibleLane: isTramTrackLane || isPublicCarLane,
                isVehicleMoving,
                suppressionFlags))
        {
            return false;
        }

        request = laneRequest;
        return true;
    }

    private static bool IsTramTrackLane(PatchedTrafficLightSystem.UpdateTrafficLightsJob job, Entity approachLaneEntity)
    {
        if (!job.m_ExtraTypeHandle.m_TrackLane.HasComponent(approachLaneEntity))
        {
            return false;
        }

        if (!job.m_ExtraTypeHandle.m_PrefabRef.TryGetComponent(approachLaneEntity, out var prefabRef))
        {
            return false;
        }

        if (!job.m_ExtraTypeHandle.m_TrackLaneData.TryGetComponent(prefabRef.m_Prefab, out var trackLaneData))
        {
            return false;
        }

        return (trackLaneData.m_TrackTypes & TrackTypes.Tram) != 0;
    }

    private static bool IsMovingTrackVehicle(
        PatchedTrafficLightSystem.UpdateTrafficLightsJob job,
        Entity vehicleEntity,
        Entity approachLaneEntity,
        Entity upstreamLaneEntity)
    {
        return job.m_ExtraTypeHandle.m_TrainCurrentLane.TryGetComponent(vehicleEntity, out var trainCurrentLane)
            && job.m_ExtraTypeHandle.m_TrainNavigation.TryGetComponent(vehicleEntity, out var trainNavigation)
            && (EarlyApproachDetection.IsEligibleTramApproachLane(
                    trainCurrentLane.m_Front.m_Lane,
                    approachLaneEntity,
                    upstreamLaneEntity,
                    Entity.Null)
                || EarlyApproachDetection.IsEligibleTramApproachLane(
                    trainCurrentLane.m_Rear.m_Lane,
                    approachLaneEntity,
                    upstreamLaneEntity,
                    Entity.Null))
            && trainNavigation.m_Speed > MovingTrainSpeedThreshold;
    }

    private static bool TryBuildEarlyApproachRequestForTrackLane(
        PatchedTrafficLightSystem.UpdateTrafficLightsJob job,
        Entity approachLaneEntity,
        TspRequest laneRequest,
        out TspRequest request)
    {
        request = default;

        if (TryBuildEarlyApproachRequestFromTrackLane(
                job,
                scanLaneEntity: approachLaneEntity,
                approachLaneEntity,
                upstreamLaneEntity: Entity.Null,
                laneRequest,
                out request))
        {
            return true;
        }

        Entity upstreamLaneEntity = TryResolveImmediateUpstreamTramLane(job, approachLaneEntity);
        return upstreamLaneEntity != Entity.Null
            && TryBuildEarlyApproachRequestFromTrackLane(
                job,
                scanLaneEntity: upstreamLaneEntity,
                approachLaneEntity,
                upstreamLaneEntity,
                laneRequest,
                out request);
    }

    private static bool TryBuildEarlyApproachRequestFromTrackLane(
        PatchedTrafficLightSystem.UpdateTrafficLightsJob job,
        Entity scanLaneEntity,
        Entity approachLaneEntity,
        Entity upstreamLaneEntity,
        TspRequest laneRequest,
        out TspRequest request)
    {
        request = default;

        if (!job.m_LaneObjects.TryGetBuffer(scanLaneEntity, out var laneObjects) || laneObjects.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < laneObjects.Length; i++)
        {
            if (TryGetMovingTransitApproachRequest(
                    job,
                    scanLaneEntity,
                    approachLaneEntity,
                    upstreamLaneEntity,
                    laneObjects[i],
                    isTramTrackLane: true,
                    isPublicCarLane: false,
                    laneRequest,
                    out request))
            {
                return true;
            }
        }

        return false;
    }

    private static Entity TryResolveImmediateUpstreamTramLane(
        PatchedTrafficLightSystem.UpdateTrafficLightsJob job,
        Entity approachLaneEntity)
    {
        if (!job.m_OwnerData.TryGetComponent(approachLaneEntity, out var owner)
            || !job.m_SubLanes.TryGetBuffer(owner.m_Owner, out var subLanes)
            || !job.m_LaneData.TryGetComponent(approachLaneEntity, out var approachLane))
        {
            return Entity.Null;
        }

        for (int i = 0; i < subLanes.Length; i++)
        {
            Entity candidateLaneEntity = subLanes[i].m_SubLane;
            if (candidateLaneEntity == approachLaneEntity
                || !IsTramTrackLane(job, candidateLaneEntity)
                || !job.m_LaneData.TryGetComponent(candidateLaneEntity, out var candidateLane))
            {
                continue;
            }

            if (candidateLane.m_EndNode.Equals(approachLane.m_StartNode))
            {
                return candidateLaneEntity;
            }
        }

        return Entity.Null;
    }

    private static bool IsMovingPublicTransportRoadVehicle(
        PatchedTrafficLightSystem.UpdateTrafficLightsJob job,
        Entity vehicleEntity,
        Entity subLaneEntity,
        LaneObject laneObject,
        TransitApproachSuppressionFlags suppressionFlags)
    {
        if (!job.m_ExtraTypeHandle.m_CarCurrentLane.TryGetComponent(vehicleEntity, out var currentLane)
            || currentLane.m_Lane != subLaneEntity
            || (currentLane.m_LaneFlags & BusStoppedLaneFlags) != 0)
        {
            return false;
        }

        float approachPosition = laneObject.m_CurvePosition.x;
        if (currentLane.m_CurvePosition.x > approachPosition)
        {
            approachPosition = currentLane.m_CurvePosition.x;
        }

        return EarlyApproachDetection.IsEligibleRoadTransitApproachState(
            isEligibleLane: true,
            hasReachedApproachThreshold: approachPosition >= BusApproachCurveThreshold,
            isBlocked: (currentLane.m_LaneFlags & VehicleCarLaneFlags.IsBlocked) != 0,
            hasReachedLaneEnd: (currentLane.m_LaneFlags & VehicleCarLaneFlags.EndReached) != 0,
            suppressionFlags: suppressionFlags);
    }

    private static TransitApproachSuppressionFlags GetSuppressionFlags(PublicTransportFlags state)
    {
        TransitApproachSuppressionFlags flags = TransitApproachSuppressionFlags.None;

        if ((state & PublicTransportFlags.Boarding) != 0)
        {
            flags |= TransitApproachSuppressionFlags.Boarding;
        }

        if ((state & PublicTransportFlags.Arriving) != 0)
        {
            flags |= TransitApproachSuppressionFlags.Arriving;
        }

        if ((state & PublicTransportFlags.RequireStop) != 0)
        {
            flags |= TransitApproachSuppressionFlags.RequireStop;
        }

        return flags;
    }

    private static TransitSignalPriorityRequest CreateRequest(
        TspRequest request,
        ushort expiryTimer,
        byte currentSignalGroup,
        byte targetSignalGroup,
        LaneSignal laneSignal)
    {
        return new TransitSignalPriorityRequest
        {
            m_TargetSignalGroup = targetSignalGroup,
            m_SourceType = (byte)request.Source,
            m_Strength = request.Strength,
            m_ExpiryTimer = expiryTimer,
            m_ExtendCurrentPhase = request.ExtensionEligible
                && currentSignalGroup == targetSignalGroup
                && (laneSignal.m_Flags & LaneSignalFlags.CanExtend) != 0,
        };
    }

    private static TspSignalRequest ToSignalRequest(TransitSignalPriorityRequest request)
    {
        return new TspSignalRequest(
            request.m_TargetSignalGroup,
            (TspSource)request.m_SourceType,
            request.m_Strength,
            request.m_ExpiryTimer,
            request.m_ExtendCurrentPhase);
    }

    private static TransitSignalPriorityRequest FromSignalRequest(TspSignalRequest request)
    {
        return new TransitSignalPriorityRequest
        {
            m_TargetSignalGroup = (byte)request.TargetSignalGroup,
            m_SourceType = (byte)request.Source,
            m_Strength = request.Strength,
            m_ExpiryTimer = request.ExpiryTimer,
            m_ExtendCurrentPhase = request.ExtendCurrentPhase,
        };
    }

    public static bool TryGetGroupedPropagatedRequest(
        PatchedTrafficLightSystem.UpdateTrafficLightsJob job,
        Entity junctionEntity,
        TrafficLights trafficLights,
        out TransitSignalPriorityRequest request)
    {
        request = default;

        if (!job.m_ExtraTypeHandle.m_GroupedTransitSignalPriorityRequest.TryGetComponent(junctionEntity, out var groupedRequest))
        {
            return false;
        }

        if (groupedRequest.m_TargetSignalGroup == 0 || groupedRequest.m_Strength <= 0f)
        {
            return false;
        }

        request = new TransitSignalPriorityRequest
        {
            m_TargetSignalGroup = groupedRequest.m_TargetSignalGroup,
            m_SourceType = groupedRequest.m_SourceType,
            m_Strength = groupedRequest.m_Strength,
            m_ExpiryTimer = groupedRequest.m_ExpiryTimer,
            m_ExtendCurrentPhase = groupedRequest.m_ExtendCurrentPhase
                && trafficLights.m_CurrentSignalGroup > 0
                && trafficLights.m_CurrentSignalGroup == groupedRequest.m_TargetSignalGroup,
        };
        return true;
    }

    public static TransitSignalPriorityRequest SelectPreferredRequest(
        TransitSignalPriorityRequest activeRequest,
        TransitSignalPriorityRequest candidateRequest,
        bool preferActiveOnTie)
    {
        if (candidateRequest.m_Strength > activeRequest.m_Strength)
        {
            return candidateRequest;
        }

        return preferActiveOnTie ? activeRequest : candidateRequest;
    }

    private static bool IsPublicOnlyCarLane(PatchedTrafficLightSystem.UpdateTrafficLightsJob job, Entity subLaneEntity)
    {
        if (!job.m_ExtraTypeHandle.m_CarLane.TryGetComponent(subLaneEntity, out var carLane))
        {
            return false;
        }

        if ((carLane.m_Flags & NetCarLaneFlags.PublicOnly) != 0)
        {
            return true;
        }

        if (!job.m_ExtraTypeHandle.m_ExtraLaneSignal.TryGetComponent(subLaneEntity, out var extraLaneSignal))
        {
            return false;
        }

        return extraLaneSignal.m_SourceSubLane != Entity.Null
            && job.m_ExtraTypeHandle.m_CarLane.TryGetComponent(extraLaneSignal.m_SourceSubLane, out var sourceCarLane)
            && (sourceCarLane.m_Flags & NetCarLaneFlags.PublicOnly) != 0;
    }

    private static byte GetTargetSignalGroup(ushort groupMask, byte currentSignalGroup)
    {
        if (currentSignalGroup > 0 && (groupMask & (1 << (currentSignalGroup - 1))) != 0)
        {
            return currentSignalGroup;
        }

        for (byte i = 1; i <= 16; i++)
        {
            if ((groupMask & (1 << (i - 1))) != 0)
            {
                return i;
            }
        }

        return 0;
    }
}
