using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Models.Systems.Physics
{
    public static class BroadphaseHelper
    {
        public const float CellSize = 40;

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
            short minX = (short) math.floor(aabb.Min.x / CellSize);
            short minY = (short) math.floor(aabb.Min.y / CellSize);
            short maxX = (short) math.floor(aabb.Max.x / CellSize);
            short maxY = (short) math.floor(aabb.Max.y / CellSize);

            for (short k = minX; k <= maxX; k++)
            for (short j = minY; j <= maxY; j++)
                yield return (k << 16) | (ushort) j;
        }
        
        public static void BuildChunks(SAPChunk chunk)
        {
            chunk.NeedRebuild = false;
            
            int freeIndex = int.MaxValue;
            
            int length = chunk.Length;
            BroadphaseAABB[] items = chunk.Items;
            
            for (int i = 0; i < length; i++)
            {
                if (items[i].Id != uint.MaxValue) 
                    continue;
                
                freeIndex = i;
                break;
            }

            if (freeIndex >= length)
                return;

            int current = freeIndex + 1;
            while (current < length)
            {
                while (current < length && items[current].Id == uint.MaxValue)
                    current++;

                if (current < length)
                    items[freeIndex++] = items[current++];
            }
            
            chunk.Length = freeIndex;
        }
        
        public static void RemoveFormChunk(SAPChunk chunk, uint entityId)
        {
            for (int i = 0; i < chunk.Length; i++)
            {
                BroadphaseAABB item = chunk.Items[i];
                if (item.Id != entityId)
                    continue;

                if (!item.IsStatic)
                    chunk.DynamicCounter--;
                
                chunk.NeedRebuild = true;
                chunk.Items[i].Id = uint.MaxValue;
                break;
            }
        }
        
        public static void UpdateChunk(SAPChunk chunk, uint entityId, AABB aabb)
        {
            for (int i = 0; i < chunk.Length; i++)
            {
                if (chunk.Items[i].Id != entityId)
                    continue;
                chunk.Items[i].AABB = aabb;
                break;
            }
        }
        
        public static void AddToChunk(SAPChunk chunk, uint entityId, AABB aabb, bool isStatic, int layer)
        {
            if (chunk.Length >= chunk.Items.Length)
                Array.Resize(ref chunk.Items, 2 * chunk.Length);

            chunk.Items[chunk.Length++] = new BroadphaseAABB
            {
                AABB = aabb,
                Id = entityId,
                IsStatic = isStatic,
                Layer = layer
            };
            if (!isStatic)
                chunk.DynamicCounter++;
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