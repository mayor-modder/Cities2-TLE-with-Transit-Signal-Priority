using System;
using System.Collections.Generic;
using Colossal;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using ColossalHash128 = Colossal.Hash128;

namespace TrafficLightsEnhancement.Serialization.Tests.Serialization;

internal sealed class SerializationWriter : IWriter
{
    private readonly List<object> _values = [];

    public IReadOnlyList<object> Values => _values;

    public Context context { get; private set; }

    public void Initialize(Context context, NativeList<byte> data, NativeArray<Entity> sharedData)
    {
        this.context = context;
    }

    public WriterBlock Begin()
    {
        return default;
    }

    public bool End(WriterBlock block)
    {
        return true;
    }

    public bool End(WriterBlock block, out int checksum)
    {
        checksum = 0;
        return true;
    }

    public void Write(byte value) => _values.Add(value);
    public void Write(sbyte value) => _values.Add(value);
    public void Write(short value) => _values.Add(value);
    public void Write(ushort value) => _values.Add(value);
    public void Write(int value) => _values.Add(value);
    public void Write(uint value) => _values.Add(value);
    public void Write(long value) => _values.Add(value);
    public void Write(ulong value) => _values.Add(value);
    public void Write(float value) => _values.Add(value);
    public void Write(double value) => _values.Add(value);
    public void Write(char value) => _values.Add(value);
    public void Write(bool value) => _values.Add(value);
    public void Write(string value) => _values.Add(value);
    public void Write(float2 value) => _values.Add(value);
    public void Write(float3 value) => _values.Add(value);
    public void Write(float4 value) => _values.Add(value);
    public void Write(int2 value) => _values.Add(value);
    public void Write(int3 value) => _values.Add(value);
    public void Write(int4 value) => _values.Add(value);
    public void Write(uint4 value) => _values.Add(value);
    public void Write(bool2 value) => _values.Add(value);
    public void Write(bool3 value) => _values.Add(value);
    public void Write(bool4 value) => _values.Add(value);
    public void Write(quaternion value) => _values.Add(value);
    public void Write(Color value) => _values.Add(value);
    public void Write(Color32 value) => _values.Add(value);
    public void Write(ColossalHash128 value) => _values.Add(value);
    public void Write(Bezier4x3 value) => _values.Add(value);

    public void Write(Entity value)
    {
        _values.Add(value.Index);
        _values.Add(value.Version);
    }

    public void Write(Entity value, bool ignoreVersion)
    {
        Write(value);
    }

    public void Write<TSerializable>(TSerializable value) where TSerializable : ISerializable
    {
        value.Serialize(this);
    }

    public void Write(NativeArray<byte> value) => throw new NotSupportedException();
    public void Write(NativeArray<byte> value, int stride) => throw new NotSupportedException();
    public void Write(NativeArray<ushort> value) => throw new NotSupportedException();
    public void Write(NativeArray<int> value) => throw new NotSupportedException();
    public void Write(NativeArray<int2> value) => throw new NotSupportedException();
    public void Write(NativeArray<float2> value) => throw new NotSupportedException();
    public void Write(NativeArray<float4> value) => throw new NotSupportedException();
    public void Write(NativeArray<Entity> value) => throw new NotSupportedException();
    public void Write(NativeList<Entity> value) => throw new NotSupportedException();
    public void Write(NativeList<int> value) => throw new NotSupportedException();
    public void Write<TSerializable>(NativeArray<TSerializable> value) where TSerializable : struct, ISerializable => throw new NotSupportedException();
}

internal sealed class SerializationReader : IReader
{
    private readonly IReadOnlyList<object> _values;
    private int _position;

    public SerializationReader(IReadOnlyList<object> values)
    {
        _values = values;
    }

    public Context context { get; private set; }

    public void Initialize(Context context, NativeArray<byte> data, NativeReference<int> position, NativeArray<Entity> sharedData)
    {
        this.context = context;
    }

    public ReaderBlock Begin()
    {
        return default;
    }

    public ReaderBlock Begin(out int size)
    {
        size = 0;
        return default;
    }

    public bool End(ReaderBlock block)
    {
        return true;
    }

    public void Skip(int size)
    {
        _position += size;
    }

    public void Read(out byte value) => value = ReadNext<byte>();
    public void Read(out sbyte value) => value = ReadNext<sbyte>();
    public void Read(out short value) => value = ReadNext<short>();
    public void Read(out ushort value) => value = ReadNext<ushort>();
    public void Read(out int value) => value = ReadNext<int>();
    public void Read(out uint value) => value = ReadNext<uint>();
    public void Read(out long value) => value = ReadNext<long>();
    public void Read(out ulong value) => value = ReadNext<ulong>();
    public void Read(out float value) => value = ReadNext<float>();
    public void Read(out double value) => value = ReadNext<double>();
    public void Read(out char value) => value = ReadNext<char>();
    public void Read(out bool value) => value = ReadNext<bool>();
    public void Read(out string value) => value = ReadNext<string>();
    public void Read(out float2 value) => value = ReadNext<float2>();
    public void Read(out float3 value) => value = ReadNext<float3>();
    public void Read(out float4 value) => value = ReadNext<float4>();
    public void Read(out int2 value) => value = ReadNext<int2>();
    public void Read(out int3 value) => value = ReadNext<int3>();
    public void Read(out int4 value) => value = ReadNext<int4>();
    public void Read(out uint4 value) => value = ReadNext<uint4>();
    public void Read(out bool2 value) => value = ReadNext<bool2>();
    public void Read(out bool3 value) => value = ReadNext<bool3>();
    public void Read(out bool4 value) => value = ReadNext<bool4>();
    public void Read(out quaternion value) => value = ReadNext<quaternion>();
    public void Read(out Color value) => value = ReadNext<Color>();
    public void Read(out Color32 value) => value = ReadNext<Color32>();
    public void Read(out ColossalHash128 value) => value = ReadNext<ColossalHash128>();
    public void Read(out Bezier4x3 value) => value = ReadNext<Bezier4x3>();

    public void Read(out Entity value)
    {
        Read(out int index);
        Read(out int version);
        value = new Entity { Index = index, Version = version };
    }

    public void Read<TSerializable>(out TSerializable value) where TSerializable : struct, ISerializable
    {
        value = default;
        value.Deserialize(this);
    }

    public void Read<TSerializable>(TSerializable value) where TSerializable : class, ISerializable
    {
        value.Deserialize(this);
    }

    public void Read(NativeArray<byte> value) => throw new NotSupportedException();
    public void Read(NativeArray<byte> value, int stride) => throw new NotSupportedException();
    public void Read(NativeArray<ushort> value) => throw new NotSupportedException();
    public void Read(NativeArray<int> value) => throw new NotSupportedException();
    public void Read(NativeArray<int2> value) => throw new NotSupportedException();
    public void Read(NativeArray<float2> value) => throw new NotSupportedException();
    public void Read(NativeArray<float4> value) => throw new NotSupportedException();
    public void Read(NativeArray<Entity> value) => throw new NotSupportedException();
    public void Read(NativeList<int> value) => throw new NotSupportedException();
    public void Read(NativeList<Entity> value) => throw new NotSupportedException();
    public void Read<TSerializable>(NativeArray<TSerializable> value) where TSerializable : struct, ISerializable => throw new NotSupportedException();

    private T ReadNext<T>()
    {
        var value = _values[_position++];
        return (T)value;
    }
}
