using System;
using System.Runtime.InteropServices;
using System.Reflection;
using C2VM.TrafficLightsEnhancement.Components;
using C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystems.Simulation;
using Game.Net;
using Unity.Entities;
using Xunit;
using TspOverrideSelection = TrafficLightsEnhancement.Logic.Tsp.TspOverrideSelection;

namespace TrafficLightsEnhancement.Ecs.Tests;

public sealed class CustomStateMachineNoTspRegressionTests
{
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
        CustomPhaseData.Options options = CustomPhaseData.Options.PrioritiseTrack)
    {
        return new CustomPhaseData
        {
            m_MinimumDuration = minimumDuration,
            m_MaximumDuration = maximumDuration,
            m_CarLaneOccupied = carLaneOccupied,
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

        public PhaseBufferScope(CustomPhaseData[] phases)
        {
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
            FieldInfo bufferField = bufferType.GetField("m_Buffer", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingFieldException(bufferType.FullName, "m_Buffer");
            FieldInfo capacityField = bufferType.GetField("m_InternalCapacity", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingFieldException(bufferType.FullName, "m_InternalCapacity");

            bufferField.SetValue(boxed, Pointer.Box(header.ToPointer(), bufferField.FieldType));
            capacityField.SetValue(boxed, internalCapacity);
            return (DynamicBuffer<CustomPhaseData>)boxed;
        }
    }
}
