using System.Collections.Generic;
using MiniEcs.Core;
using Physics;
using Unity.Mathematics;

namespace MiniEcs.Components
{
    public partial class ComponentType
    {
        public const byte Transform = 0;
        public const byte RigBody = 1;
        public const byte Collider = 2;
        public const byte RigBodyStatic = 3;
        public const byte BroadphaseRef = 4;
        public const byte BroadphaseSAP = 5;
        public const byte Ray = 6;
    }
     
    public class TransformComponent : IEcsComponent
    {
        public byte Index => ComponentType.Transform;

        public float2 Position = float2.zero;
        public float Rotation = 0;
    }

    public class RigBodyComponent : IEcsComponent
    {
        public byte Index => ComponentType.RigBody;

        public float2 Velocity = float2.zero;
        private float _mass = 1.0f;

        public float Mass
        {
            get => _mass;
            set
            {
                _mass = value;
                InvMass = !MathHelper.Equal(Mass, 0.0f) ? 1.0f / Mass : 0.0f;
            }
        }

        public float InvMass { get; private set; } = 0.1f;
    }

    public enum ColliderType : byte
    {
        Circle = 0,
        Rect = 1
    }

    public abstract class ColliderComponent : IEcsComponent
    {
        public byte Index => ComponentType.Collider;

        public abstract float2 Size { get; }
        public abstract ColliderType ColliderType { get; }
        public int Layer { get; set; }
    }

    public class RectColliderComponent : ColliderComponent
    {
        public float2x4 Vertices { get; private set; }
        public float2x4 Normals { get; private set; }

        private float2 _size;
        public float2 RectSize
        {
            get => _size;
            set
            {
                _size = value;
                float w = _size.x;
                float h = _size.y;

                Vertices = new float2x4(-w, w, w, -w, -h, -h, h, h);
                Normals = new float2x4(0.0f, 1.0f, 0.0f, -1.0f, -1.0f, 0.0f, 1.0f, 0.0f);
            }
        }

        public override float2 Size => _size;
        public override ColliderType ColliderType => ColliderType.Rect;
    }

    public class CircleColliderComponent : ColliderComponent
    {
        public float Radius;
        public override float2 Size => Radius;
        public override ColliderType ColliderType => ColliderType.Circle;
    }

    public class RigBodyStaticComponent : IEcsComponent
    {
        public byte Index => ComponentType.RigBodyStatic;
    }

    public class BroadphaseRefComponent : IEcsComponent
    {
        public byte Index => ComponentType.BroadphaseRef;

        public List<SAPChunk> Chunks;
        public int ChunksHash;
        public AABB AABB;
    }

    public class BroadphaseSAPComponent : IEcsComponent
    {
        public byte Index => ComponentType.BroadphaseSAP;

        public readonly Dictionary<int, SAPChunk> Chunks = new Dictionary<int, SAPChunk>();
        public readonly HashSet<BroadphasePair> Pairs = new HashSet<BroadphasePair>();
    }

    public class RayComponent : IEcsComponent
    {
        public byte Index => ComponentType.Ray;

        public int Layer;
        public float2 Source;
        public float Rotation;
        public float Length;

        public float2 Target
        {
            get
            {
                float2 dir = new float2(-math.sin(Rotation), math.cos(Rotation));
                return Source + Length * dir;
            }
        }

        public bool Hit;
        public float2 HitPoint;
    }

    
}