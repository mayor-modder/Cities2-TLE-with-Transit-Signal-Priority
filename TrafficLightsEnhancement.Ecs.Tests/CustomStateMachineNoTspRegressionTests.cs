using System;
using System.Runtime.InteropServices;
using System.Reflection;
using C2VM.TrafficLightsEnhancement.Components;
using C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystems.Simulation;
using Game.Net;
using Unity.Entities;
using Unity.Mathematics;
using Xunit;
using TspOverrideSelection = TrafficLightsEnhancement.Logic.Tsp.TspOverrideSelection;
using TspSource = TrafficLightsEnhancement.Logic.Tsp.TspSource;

namespace TrafficLightsEnhancement.Ecs.Tests;

public sealed class CustomStateMachineNoTspRegressionTests
{
    [Fact]
    public void Synthetic_phase_buffer_layout_guard_matches_current_dots()
    {
        PhaseBufferScope.AssertLayoutCompatible();
    }

    [Fact]
    public void Get_next_signal_group_empty_buffer_matches()
    {
        CompareNextGroup(
            currentGroup: 1,
            new CustomTrafficLights(CustomTrafficLights.Patterns.CustomPhase),
            Array.Empty<CustomPhaseData>());
    }

    [Fact]
    public void Get_next_signal_group_manual_override_matches()
    {
        var custom = new CustomTrafficLights(CustomTrafficLights.Patterns.CustomPhase);
        custom.m_ManualSignalGroup = 2;

        CompareNextGroup(
            currentGroup: 1,
            custom,
            Phase(),
            Phase(),
            Phase());
    }

    [Fact]
    public void Get_next_signal_group_fixed_timed_sequential_matches()
    {
        var custom = new CustomTrafficLights(CustomTrafficLights.Patterns.CustomPhase, CustomTrafficLights.TrafficMode.FixedTimed);
        custom.SetOptions(CustomTrafficLights.TrafficOptions.None);

        CompareNextGroup(
            currentGroup: 1,
            custom,
            Phase(),
            Phase(),
            Phase());
    }

    [Fact]
    public void Get_next_signal_group_fixed_timed_smart_selection_matches()
    {
        var custom = new CustomTrafficLights(CustomTrafficLights.Patterns.CustomPhase, CustomTrafficLights.TrafficMode.FixedTimed);

        CompareNextGroup(
            currentGroup: 1,
            custom,
            Phase(minimumDuration: 2),
            Phase(minimumDuration: 0, carLaneOccupied: 2),
            Phase(minimumDuration: 3));
    }

    [Fact]
    public void Get_next_signal_group_dynamic_positive_minimum_matches()
    {
        CompareNextGroup(
            currentGroup: 1,
            new CustomTrafficLights(CustomTrafficLights.Patterns.CustomPhase, CustomTrafficLights.TrafficMode.Dynamic),
            Phase(minimumDuration: 2),
            Phase(minimumDuration: 3),
            Phase(minimumDuration: 0));
    }

    [Fact]
    public void Get_next_signal_group_dynamic_skips_zero_minimum_without_demand_matches()
    {
        CompareNextGroup(
            currentGroup: 1,
            new CustomTrafficLights(CustomTrafficLights.Patterns.CustomPhase, CustomTrafficLights.TrafficMode.Dynamic),
            Phase(minimumDuration: 2),
            Phase(minimumDuration: 0),
            Phase(minimumDuration: 3));
    }

    [Fact]
    public void Get_next_signal_group_dynamic_selects_zero_minimum_with_demand_matches()
    {
        CompareNextGroup(
            currentGroup: 1,
            new CustomTrafficLights(CustomTrafficLights.Patterns.CustomPhase, CustomTrafficLights.TrafficMode.Dynamic),
            Phase(minimumDuration: 2),
            Phase(minimumDuration: 0, carLaneOccupied: 1),
            Phase(minimumDuration: 3));
    }

    [Fact]
    public void Linked_chain_advances_to_prioritized_later_linked_phase()
    {
        var custom = new CustomTrafficLights(CustomTrafficLights.Patterns.CustomPhase, CustomTrafficLights.TrafficMode.FixedTimed);
        custom.SetOptions(CustomTrafficLights.TrafficOptions.None);
        using var phases = new PhaseBufferScope(
            Phase(options: CustomPhaseData.Options.LinkedWithNextPhase),
            Phase(minimumDuration: 0, options: CustomPhaseData.Options.LinkedWithNextPhase),
            Phase(minimumDuration: 0, priority: 1));

        byte nextGroup = CustomStateMachine.GetNextSignalGroup(
            currentGroup: 1,
            phases.Buffer,
            custom,
            out bool linked);

        Assert.Equal(3, nextGroup);
        Assert.True(linked);
    }

    [Fact]
    public void Linked_predecessor_rewinds_base_selection_to_start_linked_block()
    {
        using var phases = new PhaseBufferScope(
            Phase(carFlow: 0),
            Phase(minimumDuration: 0, carLaneOccupied: 1, carFlow: 1, options: CustomPhaseData.Options.LinkedWithNextPhase),
            Phase(minimumDuration: 0, carLaneOccupied: 3, carFlow: 3));

        byte nextGroup = CustomStateMachine.GetNextSignalGroup(
            currentGroup: 1,
            phases.Buffer,
            new CustomTrafficLights(CustomTrafficLights.Patterns.CustomPhase, CustomTrafficLights.TrafficMode.FixedTimed),
            out bool linked);

        Assert.Equal(2, nextGroup);
        Assert.True(linked);
    }

    [Fact]
    public void Tsp_override_clears_linked_flag_when_it_changes_linked_base_selection()
    {
        var custom = new CustomTrafficLights(CustomTrafficLights.Patterns.CustomPhase, CustomTrafficLights.TrafficMode.FixedTimed);
        custom.SetOptions(CustomTrafficLights.TrafficOptions.None);
        using var phases = new PhaseBufferScope(
            Phase(options: CustomPhaseData.Options.LinkedWithNextPhase),
            Phase(minimumDuration: 0, options: CustomPhaseData.Options.LinkedWithNextPhase),
            Phase(minimumDuration: 0, priority: 1));

        byte nextGroup = CustomStateMachine.GetNextSignalGroup(
            currentGroup: 1,
            phases.Buffer,
            custom,
            out bool linked,
            out TspOverrideSelection tspSelection,
            hasTspRequest: true,
            tspRequest: new TransitSignalPriorityRequest
            {
                m_TargetSignalGroup = 2,
                m_SourceType = (byte)TspSource.Track,
                m_Strength = 1f,
                m_ExpiryTimer = 10
            });

        Assert.Equal(2, nextGroup);
        Assert.True(tspSelection.Applied);
        Assert.False(linked);
    }

    [Fact]
    public void Linked_phase_transition_resets_skipped_unprioritized_linked_phase_counter()
    {
        var trafficLights = TrafficLights(TrafficLightState.Ongoing, currentGroup: 1, nextGroup: 0);
        var custom = new CustomTrafficLights(CustomTrafficLights.Patterns.CustomPhase, CustomTrafficLights.TrafficMode.FixedTimed);
        custom.SetOptions(CustomTrafficLights.TrafficOptions.None);
        using var phases = new PhaseBufferScope(
            Phase(options: CustomPhaseData.Options.LinkedWithNextPhase | CustomPhaseData.Options.EndPhasePrematurely),
            Phase(minimumDuration: 0, turnsSinceLastRun: 5, options: CustomPhaseData.Options.LinkedWithNextPhase),
            Phase(minimumDuration: 0, priority: 1, turnsSinceLastRun: 7));

        bool updated = CustomStateMachine.UpdateTrafficLightState(
            ref trafficLights,
            ref custom,
            phases.Buffer);

        Assert.True(updated);
        Assert.Equal(TrafficLightState.Ending, trafficLights.m_State);
        Assert.Equal(3, trafficLights.m_NextSignalGroup);
        Assert.Equal(0, phases.Buffer[1].m_TurnsSinceLastRun);
        Assert.Equal(7, phases.Buffer[2].m_TurnsSinceLastRun);
    }

    [Theory]
    [InlineData(TrafficLightState.None, 0, 0, 0)]
    [InlineData(TrafficLightState.Extending, 1, 0, 0)]
    [InlineData(TrafficLightState.Extended, 1, 0, 0)]
    [InlineData(TrafficLightState.Beginning, 0, 1, 0)]
    [InlineData(TrafficLightState.Ongoing, 1, 0, 1)]
    [InlineData(TrafficLightState.Ongoing, 1, 0, 20)]
    [InlineData(TrafficLightState.Ending, 1, 2, 0)]
    [InlineData(TrafficLightState.Changing, 1, 2, 0)]
    public void Update_state_legacy_and_tsp_aware_no_request_match(
        TrafficLightState state,
        byte currentGroup,
        byte nextGroup,
        byte customTimer)
    {
        var trafficLights = TrafficLights(state, currentGroup, nextGroup);
        var custom = new CustomTrafficLights(CustomTrafficLights.Patterns.CustomPhase, CustomTrafficLights.TrafficMode.FixedTimed)
        {
            m_Timer = customTimer
        };

        CompareStateUpdate(
            trafficLights,
            custom,
            Phase(minimumDuration: 2, maximumDuration: 20),
            Phase(minimumDuration: 2, maximumDuration: 20));
    }

    [Fact]
    public void Update_state_end_phase_prematurely_matches()
    {
        CompareStateUpdate(
            TrafficLights(TrafficLightState.Ongoing, currentGroup: 1, nextGroup: 0),
            new CustomTrafficLights(CustomTrafficLights.Patterns.CustomPhase, CustomTrafficLights.TrafficMode.FixedTimed),
            Phase(minimumDuration: 2, maximumDuration: 20, options: CustomPhaseData.Options.EndPhasePrematurely),
            Phase(minimumDuration: 2, maximumDuration: 20));
    }

    private static void CompareNextGroup(byte currentGroup, CustomTrafficLights custom, params CustomPhaseData[] phases)
    {
        using var legacy = new PhaseBufferScope(phases);
        using var tspAware = new PhaseBufferScope(phases);

        byte legacyNext = CustomStateMachine.GetNextSignalGroup(currentGroup, legacy.Buffer, custom, out bool legacyLinked);
        byte tspAwareNext = CustomStateMachine.GetNextSignalGroup(
            currentGroup,
            tspAware.Buffer,
            custom,
            out bool tspAwareLinked,
            out TspOverrideSelection tspSelection,
            hasTspRequest: false,
            tspRequest: default);

        Assert.Equal(legacyNext, tspAwareNext);
        Assert.Equal(legacyLinked, tspAwareLinked);
        Assert.False(tspSelection.Applied);
        AssertBuffersEqual(legacy.Buffer, tspAware.Buffer);
    }

    private static void CompareStateUpdate(TrafficLights trafficLights, CustomTrafficLights custom, params CustomPhaseData[] phases)
    {
        using var legacy = new PhaseBufferScope(phases);
        using var tspAware = new PhaseBufferScope(phases);

        TrafficLights legacyTrafficLights = trafficLights;
        TrafficLights tspAwareTrafficLights = trafficLights;
        CustomTrafficLights legacyCustom = custom;
        CustomTrafficLights tspAwareCustom = custom;

        bool legacyUpdated = CustomStateMachine.UpdateTrafficLightState(
            ref legacyTrafficLights,
            ref legacyCustom,
            legacy.Buffer);
        bool tspAwareUpdated = CustomStateMachine.UpdateTrafficLightState(
            ref tspAwareTrafficLights,
            ref tspAwareCustom,
            tspAware.Buffer,
            tspAware.Buffer,
            C2VM.TrafficLightsEnhancement.Components.TransitSignalPrioritySettings.CreateDefault(),
            hasTspRequest: false,
            tspRequest: default,
            out TspOverrideSelection tspSelection);

        Assert.Equal(legacyUpdated, tspAwareUpdated);
        Assert.False(tspSelection.Applied);
        AssertStructFieldsEqual(legacyTrafficLights, tspAwareTrafficLights);
        AssertStructFieldsEqual(legacyCustom, tspAwareCustom);
        AssertBuffersEqual(legacy.Buffer, tspAware.Buffer);
    }

    private static TrafficLights TrafficLights(TrafficLightState state, byte currentGroup, byte nextGroup)
    {
        return new TrafficLights
        {
            m_State = state,
            m_SignalGroupCount = 2,
            m_CurrentSignalGroup = currentGroup,
            m_NextSignalGroup = nextGroup,
            m_Timer = 0
        };
    }

    private static CustomPhaseData Phase(
        ushort minimumDuration = 2,
        ushort maximumDuration = 20,
        ushort carLaneOccupied = 0,
        int priority = 0,
        ushort turnsSinceLastRun = 0,
        float currentFlow = 0f,
        float currentWait = 0f,
        float carFlow = 0f,
        CustomPhaseData.Options options = CustomPhaseData.Options.PrioritiseTrack)
    {
        return new CustomPhaseData
        {
            m_MinimumDuration = minimumDuration,
            m_MaximumDuration = maximumDuration,
            m_CarLaneOccupied = carLaneOccupied,
            m_Priority = priority,
            m_TurnsSinceLastRun = turnsSinceLastRun,
            m_CurrentFlow = currentFlow,
            m_CurrentWait = currentWait,
            m_CarFlow = new float3(carFlow, carFlow, carFlow),
            m_Options = options
        };
    }

    private static void AssertBuffersEqual(DynamicBuffer<CustomPhaseData> expected, DynamicBuffer<CustomPhaseData> actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            AssertStructFieldsEqual(expected[i], actual[i]);
        }
    }

    private static void AssertStructFieldsEqual<T>(T expected, T actual)
    {
        foreach (FieldInfo field in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            Assert.Equal(field.GetValue(expected), field.GetValue(actual));
        }
    }

    private sealed unsafe class PhaseBufferScope : IDisposable
    {
        private const int BufferHeaderSize = 16;
        private const int BufferHeaderPointerOffset = 0;
        private const int BufferHeaderLengthOffset = 8;
        private const int BufferHeaderCapacityOffset = 12;

        private readonly IntPtr _header;
        private readonly IntPtr _data;

        public PhaseBufferScope(params CustomPhaseData[] phases)
        {
            AssertLayoutCompatible();

            // Unity World allocation calls engine ECalls that are unavailable under dotnet test.
            // DynamicBuffer<T> itself only needs a BufferHeader pointer for these no-resize cases.
            int elementSize = Marshal.SizeOf(typeof(CustomPhaseData));
            int capacity = Math.Max(phases.Length, 1);
            _header = Marshal.AllocHGlobal(BufferHeaderSize);
            _data = Marshal.AllocHGlobal(elementSize * capacity);

            Marshal.WriteIntPtr(_header, BufferHeaderPointerOffset, _data);
            Marshal.WriteInt32(_header, BufferHeaderLengthOffset, phases.Length);
            Marshal.WriteInt32(_header, BufferHeaderCapacityOffset, capacity);

            for (int i = 0; i < phases.Length; i++)
            {
                Marshal.StructureToPtr(phases[i], IntPtr.Add(_data, i * elementSize), fDeleteOld: false);
            }

            Buffer = CreateBuffer(_header, capacity);
        }

        public DynamicBuffer<CustomPhaseData> Buffer { get; }

        public void Dispose()
        {
            if (_data != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_data);
            }

            if (_header != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_header);
            }
        }

        private static DynamicBuffer<CustomPhaseData> CreateBuffer(IntPtr header, int internalCapacity)
        {
            object boxed = default(DynamicBuffer<CustomPhaseData>);
            Type bufferType = typeof(DynamicBuffer<CustomPhaseData>);
            FieldInfo bufferField = RequireField(bufferType, "m_Buffer");
            FieldInfo capacityField = RequireField(bufferType, "m_InternalCapacity");

            bufferField.SetValue(boxed, Pointer.Box(header.ToPointer(), bufferField.FieldType));
            capacityField.SetValue(boxed, internalCapacity);
            return (DynamicBuffer<CustomPhaseData>)boxed;
        }

        public static void AssertLayoutCompatible()
        {
            Type bufferType = typeof(DynamicBuffer<CustomPhaseData>);
            FieldInfo bufferField = RequireField(bufferType, "m_Buffer");
            FieldInfo capacityField = RequireField(bufferType, "m_InternalCapacity");

            Assert.True(
                bufferField.FieldType.IsPointer,
                "DynamicBuffer<T>.m_Buffer must remain a pointer field for the synthetic ECS test buffer.");
            Assert.Equal(typeof(int), capacityField.FieldType);

            Type headerType = bufferField.FieldType.GetElementType()
                ?? throw new InvalidOperationException("DynamicBuffer<T>.m_Buffer pointer element type is unavailable.");
            Assert.Equal("Unity.Entities.BufferHeader", headerType.FullName);

            Assert.Equal(8, IntPtr.Size);
            Assert.Equal(BufferHeaderSize, IntPtr.Size + sizeof(int) + sizeof(int));
            AssertBufferHeaderOffset(headerType, "Pointer", BufferHeaderPointerOffset);
            AssertBufferHeaderOffset(headerType, "Length", BufferHeaderLengthOffset);
            AssertBufferHeaderOffset(headerType, "Capacity", BufferHeaderCapacityOffset);
        }

        private static FieldInfo RequireField(Type type, string name)
        {
            return type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingFieldException(type.FullName, name);
        }

        private static void AssertBufferHeaderOffset(Type headerType, string fieldName, int expectedOffset)
        {
            int actualOffset = Marshal.OffsetOf(headerType, fieldName).ToInt32();
            Assert.Equal(expectedOffset, actualOffset);
        }
    }
}
