using Colossal.UI.Binding;
using Unity.Mathematics;
using UnityEngine;

namespace C2VM.TrafficLightsEnhancement.Systems.UI;

public static class UITypes
{
    public struct WorldPosition : IJsonWritable
    {
        public float x;
        public float y;
        public float z;

        public readonly string key => $"{x:0.0},{y:0.0},{z:0.0}";

        public static implicit operator WorldPosition(float pos) => new() { x = pos, y = pos, z = pos };

        public static implicit operator WorldPosition(float3 pos) => new() { x = pos.x, y = pos.y, z = pos.z };

        public static implicit operator float3(WorldPosition pos) => new(pos.x, pos.y, pos.z);

        public static implicit operator Vector3(WorldPosition pos) => new(pos.x, pos.y, pos.z);

        public static implicit operator string(WorldPosition pos) => pos.key;

        public readonly void Write(IJsonWriter writer)
        {
            writer.TypeBegin(typeof(WorldPosition).FullName);
            writer.PropertyName(nameof(x));
            writer.Write(x);
            writer.PropertyName(nameof(y));
            writer.Write(y);
            writer.PropertyName(nameof(z));
            writer.Write(z);
            writer.PropertyName(nameof(key));
            writer.Write(key);
            writer.TypeEnd();
        }
    }
}
