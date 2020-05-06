using System.Collections.Generic;
using Unity.Mathematics;

namespace Models.Systems.Physics
{
    public static class BroadphaseHelper
    {
        public const float CellSize = 40;

        public static SAPChunk GetOrCreateChunk(int chunkId, BroadphaseSAPComponent bpChunks)
        {
            if (bpChunks.Items.TryGetValue(chunkId, out SAPChunk bpChunk))
                return bpChunk;

            bpChunk = new SAPChunk();
            bpChunks.Items.Add(chunkId, bpChunk);

            return bpChunk;
        }

        public static IEnumerable<SAPChunk> GetChunks(AABB aabb, BroadphaseSAPComponent bpChunks)
        {
            short minX = (short) math.floor(aabb.Min.x / CellSize);
            short minY = (short) math.floor(aabb.Min.y / CellSize);
            short maxX = (short) math.floor(aabb.Max.x / CellSize);
            short maxY = (short) math.floor(aabb.Max.y / CellSize);

            for (short k = minX; k <= maxX; k++)
            for (short j = minY; j <= maxY; j++)
                yield return GetOrCreateChunk((k << 16) | (ushort) j, bpChunks);
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
                if (chunk.Items[i].Id != entityId)
                    continue;

                chunk.NeedRebuild = true;
                chunk.IsDirty = true;
                chunk.Items[i].Id = uint.MaxValue;
                break;
            }
        }
    }
}