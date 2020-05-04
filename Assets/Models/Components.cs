using System.Collections.Generic;
using MiniEcs.Core;
using Unity.Mathematics;

namespace Models
{
    public static class ComponentType
    {
        public const byte Translation = 0;
        public const byte Rotation = 1;
        public const byte RigBody = 2;
        public const byte Collider = 3;
        public const byte RigBodyStatic = 4;
        public const byte BroadphaseRef = 5;
        public const byte BroadphaseSAP = 6;
        public const byte Ray = 7;
        
        public const byte TotalComponents = 8;
    }

    public class TranslationComponent : IEcsComponent
    {
        public byte Index => ComponentType.Translation;
        public float2 Value = float2.zero;
    }
    
    public class RotationComponent : IEcsComponent
    {
        public byte Index => ComponentType.Rotation;
        public float Value = 0;
    }
    
    public class RigBodyComponent : IEcsComponent
    {
        public byte Index => ComponentType.RigBody;
        
        public float2 Velocity = float2.zero;
        public float AngularVelocity = 0;

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

        private float _inertia = 1.0f;
        public float Inertia
        {
            get => _inertia;
            set
            {
                _inertia = value;
                InvInertia = !MathHelper.Equal(Inertia, 0.0f) ? 1.0f / Inertia : 0.0f;
            }
        }
        public float InvInertia { get; private set; } = 0.1f;       

        
        public void ApplyForce(float2 f)
        {
            Velocity += f * InvMass;
        }
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
        public float2x4 Vertices;
        public float2x4 Normals;

        public RectColliderComponent(float2 size)
        {
            Size = size;
            float w = size.x;
            float h = size.y;
			
            Vertices = new float2x4(-w, w, w, -w, -h, -h, h, h);
            Normals = new float2x4(0.0f, 1.0f, 0.0f, -1.0f, -1.0f, 0.0f, 1.0f, 0.0f);
        }

        public override float2 Size { get; }
        public override ColliderType ColliderType => ColliderType.Rect;
    }
    
    public class CircleColliderComponent : ColliderComponent
    {
        public readonly float Radius;
        public CircleColliderComponent(float r)
        {
            Radius = r;
        }

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
        public List<SAPChunk> Items;
    }

    public class SAPChunk
    {
        public int Length;
        public bool NeedRebuild;
        
        public BroadphaseAABB[] Items = new BroadphaseAABB[32];
        public long[] Pairs = new long[32];
        public int PairLength;
        public int SortAxis;
        public bool IsDirty;
    }
    
    public struct BroadphaseAABB
    {
        public uint Id;
        public int Layer;
        public bool IsStatic;
        public AABB AABB;
    }
    
    public class BroadphaseSAPComponent : IEcsComponent
    {
        public byte Index => ComponentType.BroadphaseSAP;
        public readonly Dictionary<int, SAPChunk> Items = new Dictionary<int, SAPChunk>();
        public readonly HashSet<long> Pairs = new HashSet<long>();
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