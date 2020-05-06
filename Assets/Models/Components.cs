using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MiniEcs.Core;
using Unity.Mathematics;

namespace Models
{
    public static class ComponentType
    {
        public const byte Transform = 0;
        public const byte RigBody = 1;
        public const byte Collider = 2;
        public const byte RigBodyStatic = 3;
        public const byte BroadphaseRef = 4;
        public const byte BroadphaseSAP = 5;
        public const byte Ray = 6;

        public const byte Hero = 7;
        public const byte Character = 8;
        
        public const byte StaticRect = 9;
        public const byte StaticCircle = 10;
        public const byte BlueCircle = 11;
        public const byte YellowCircle = 12;
        
        public const byte TotalComponents = 13;
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
    
    public struct BroadphasePair : IEquatable<BroadphasePair>
    {
        public EcsEntity EntityA { get; }
        public EcsEntity EntityB { get; }

        public BroadphasePair(EcsEntity entityA, EcsEntity entityB)
        {
            EntityA = entityA;
            EntityB = entityB;
        }

        public bool Equals(BroadphasePair other)
        {
            return EntityA == other.EntityA && EntityB == other.EntityB ||
                   EntityB == other.EntityA && EntityA == other.EntityB;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)EntityA.Id;
                hash = (hash * 397) ^ (int)EntityB.Id;
                return hash;
            }
        }
        
    }
    
    public class BroadphaseRefComponent : IEcsComponent
    {
        public byte Index => ComponentType.BroadphaseRef;
        public List<SAPChunk> Chunks;
        public int ChunksHash;
        public AABB AABB;
    }

    public class SAPChunk
    {
        public int Id { get; }
        public int Length;
        public bool NeedRebuild;
        
        public BroadphaseAABB[] Items = new BroadphaseAABB[32];
        public BroadphasePair[] Pairs = new BroadphasePair[32];
        public int PairLength;
        public int SortAxis;
        public int DynamicCounter;
        public bool IsDirty = true;

        public SAPChunk(int id)
        {
            Id = id;
        }
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct BroadphaseAABB
    {
        public EcsEntity Entity;
        public uint Id;
        public int Layer;
        public bool IsStatic;
        public AABB* AABB;
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

    public class HeroComponent : IEcsComponent
    {
        public byte Index => ComponentType.Hero;
    }

    public class CharacterComponent : IEcsComponent
    {
        public byte Index => ComponentType.Character;
        public Character Ref;
    }

    public class StaticRectComponent : IEcsComponent
    {
        public byte Index => ComponentType.StaticRect;
    }
    
    public class StaticCircleComponent : IEcsComponent
    {
        public byte Index => ComponentType.StaticCircle;
    }
    
    public class BlueCircleComponent : IEcsComponent
    {
        public byte Index => ComponentType.BlueCircle;
    }
    
    public class YellowCircleComponent : IEcsComponent
    {
        public byte Index => ComponentType.YellowCircle;
    }

}