using System;
using System.Collections.Generic;
using MiniEcs.Core;
using MiniEcs.Core.Systems;

namespace Models.Systems.Physics
{
    [EcsUpdateInGroup(typeof(PhysicsSystemGroup))]
    [EcsUpdateAfter(typeof(IntegrateVelocitySystem))]
    [EcsUpdateBefore(typeof(BroadphaseCalculatePairSystem))]
    public class BroadphaseUpdateSystem : IEcsSystem
    {
        private readonly EcsFilter _entitiesFilter;
        public BroadphaseUpdateSystem()
        {
            _entitiesFilter = new EcsFilter().AllOf(ComponentType.Transform, ComponentType.Collider, ComponentType.RigBody,
                ComponentType.BroadphaseRef).NoneOf(ComponentType.RigBodyStatic);
        }

        public unsafe void Update(float deltaTime, EcsWorld world)
        {
            BroadphaseSAPComponent bpChunks =
                world.GetOrCreateSingleton<BroadphaseSAPComponent>(ComponentType.BroadphaseSAP); 

            foreach (EcsEntity entity in world.Filter(_entitiesFilter))
            {
                uint entityId = entity.Id;

                TransformComponent tr = (TransformComponent) entity[ComponentType.Transform];
                ColliderComponent col = (ColliderComponent) entity[ComponentType.Collider];
                RigBodyComponent rig = (RigBodyComponent) entity[ComponentType.RigBody];
                BroadphaseRefComponent bpRef = (BroadphaseRefComponent) entity[ComponentType.BroadphaseRef];
                
                AABB aabb = new AABB(col.Size, tr.Position, col.ColliderType == ColliderType.Rect ? tr.Rotation : 0f);
                fixed (AABB* pAABB = &bpRef.AABB)
                {
                    pAABB->Min = aabb.Min;
                    pAABB->Max = aabb.Max;
                }

                int chunksHash = BroadphaseHelper.CalculateChunksHash(aabb);                
                if (bpRef.ChunksHash == chunksHash)
                    continue;
                
                bool isStatic = MathHelper.Equal(rig.InvMass, 0);
                int layer = col.Layer;
                
                List<SAPChunk> chunks = bpRef.Chunks;
                List<SAPChunk> newChunks = new List<SAPChunk>(4);
                foreach (int chunkId in BroadphaseHelper.GetChunks(aabb))
                {
                    int index = -1;
                    for (int i = 0; i < chunks.Count; i++)
                    {
                        SAPChunk chunk = chunks[i];
                        if (chunk == null || chunk.Id != chunkId)
                            continue;

                        index = i;
                        break;
                    }

                    if (index >= 0)
                    {
                        SAPChunk chunk = chunks[index];
                        chunks[index] = null;
                        newChunks.Add(chunk);
                    }
                    else
                    {
                        SAPChunk chunk = BroadphaseHelper.GetOrCreateChunk(chunkId, bpChunks);

                        if (chunk.Length >= chunk.Items.Length)
                            Array.Resize(ref chunk.Items, 2 * chunk.Length);

                        fixed (AABB* pAABB = &bpRef.AABB)
                        {
                            chunk.Items[chunk.Length++] = new BroadphaseAABB
                            {
                                AABB = pAABB,
                                Id = entityId,
                                IsStatic = isStatic,
                                Layer = layer,
                                Entity = entity
                            };
                        }

                        if (!isStatic)
                            chunk.DynamicCounter++;

                        newChunks.Add(chunk);
                    }
                }

                foreach (SAPChunk chunk in chunks)
                {
                    if (chunk == null)
                        continue;
                    BroadphaseHelper.RemoveFormChunk(chunk, entityId);
                }

                bpRef.Chunks = newChunks;
                bpRef.ChunksHash = chunksHash;
            }
        }
    }
}