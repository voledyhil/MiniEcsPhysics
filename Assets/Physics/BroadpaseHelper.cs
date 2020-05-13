using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MiniEcs.Core;
using Unity.Mathematics;

namespace Physics
{
    public class SAPChunk
    {
        public readonly int Id;
        public int Length;
        public int FreeIndex = int.MaxValue;
        public int PairLength;
        
        public BroadphaseAABB[] Items = new BroadphaseAABB[32];
        public BroadphasePair[] Pairs = new BroadphasePair[32];
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
        public IEcsEntity Entity;
        public uint Id;
        public int Layer;
        public bool IsStatic;
        public AABB* AABB;
    }
    
    public struct BroadphasePair : IEquatable<BroadphasePair>
    {
        public IEcsEntity EntityA { get; }
        public IEcsEntity EntityB { get; }

        public BroadphasePair(IEcsEntity entityA, IEcsEntity entityB)
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
    
    public static class BroadphaseHelper
    {
        public const float ChunkSize = 50;

        public static SAPChunk GetOrCreateChunk(int chunkId, BroadphaseSAPComponent bpChunks)
        {
            if (bpChunks.Chunks.TryGetValue(chunkId, out SAPChunk bpChunk))
                return bpChunk;

            bpChunk = new SAPChunk(chunkId);
            bpChunks.Chunks.Add(chunkId, bpChunk);

            return bpChunk;
        }
        
        public static IEnumerable<int> GetChunks(AABB aabb)
        {
            short minX = (short) math.floor(aabb.Min.x / ChunkSize);
            short minY = (short) math.floor(aabb.Min.y / ChunkSize);
            short maxX = (short) math.floor(aabb.Max.x / ChunkSize);
            short maxY = (short) math.floor(aabb.Max.y / ChunkSize);

            for (short k = minX; k <= maxX; k++)
            for (short j = minY; j <= maxY; j++)
                yield return (k << 16) | (ushort) j;
        }
        
        public static void BuildChunks(SAPChunk chunk)
        {
            int length = chunk.Length;
            int freeIndex = chunk.FreeIndex;
            if (freeIndex >= length)
                return;

            BroadphaseAABB[] items = chunk.Items;
            int current = freeIndex + 1;
            while (current < length)
            {
                while (current < length && items[current].Id == uint.MaxValue)
                    current++;

                if (current < length)
                    items[freeIndex++] = items[current++];
            }
            
            chunk.Length = freeIndex;
            chunk.FreeIndex = int.MaxValue;
        }

        public static void RemoveFormChunk(SAPChunk chunk, uint entityId)
        {
            int index = Array.FindIndex(chunk.Items, 0, chunk.Length, bp => bp.Id == entityId);
            if (index < 0)
                throw new InvalidOperationException($"entity by id '{entityId}' not found in chunk '{chunk.Id}'");
            
            BroadphaseAABB item = chunk.Items[index];
            
            if (!item.IsStatic)
                chunk.DynamicCounter--;
            
            item.Id = uint.MaxValue;
            item.Entity = null;
            
            chunk.Items[index] = item;
            chunk.FreeIndex = math.min(chunk.FreeIndex, index);
        }

        public static int CalculateChunksHash(AABB aabb)
        {
            int hash = 647;
            int counter = 0;
            foreach (int chunkId in GetChunks(aabb))
            {
                hash ^= chunkId * 307;
                counter++;
            }
            hash ^= counter * 367;
            return hash;
        }
    }
}