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
            _entitiesFilter = new EcsFilter().AllOf(ComponentType.Translation, ComponentType.Rotation,
                ComponentType.Collider, ComponentType.RigBody, ComponentType.BroadphaseRef).NoneOf(ComponentType.RigBodyStatic);
        }

        public void Update(float deltaTime, EcsWorld world)
        {
            BroadphaseSAPComponent bpChunks =
                world.GetOrCreateSingleton<BroadphaseSAPComponent>(ComponentType.BroadphaseSAP); 

            foreach (EcsEntity entity in world.Filter(_entitiesFilter))
            {
                uint entityId = entity.Id;

                TranslationComponent tr = (TranslationComponent) entity[ComponentType.Translation];
                RotationComponent rot = (RotationComponent) entity[ComponentType.Rotation];
                ColliderComponent col = (ColliderComponent) entity[ComponentType.Collider];
                RigBodyComponent rig = (RigBodyComponent) entity[ComponentType.RigBody];
                BroadphaseRefComponent brRef = (BroadphaseRefComponent) entity[ComponentType.BroadphaseRef];
                
                AABB aabb = new AABB(col.Size, tr.Value, col.ColliderType == ColliderType.Rect ? rot.Value : 0f);
                bool isStatic = MathHelper.Equal(rig.InvMass, 0);
                int layer = col.Layer;
                
                List<SAPChunk> chunks = brRef.Chunks;
                int chunksHash = BroadphaseHelper.CalculateChunksHash(aabb);                
                if (brRef.ChunksHash == chunksHash)
                {
                    foreach (SAPChunk chunk in chunks)
                    {
                        BroadphaseHelper.UpdateChunk(chunk, entityId, aabb);
                    }
                    continue;
                }
                
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
                        
                        BroadphaseHelper.UpdateChunk(chunk, entityId, aabb);
                    }
                    else
                    {
                        SAPChunk chunk = BroadphaseHelper.GetOrCreateChunk(chunkId, bpChunks);
                        BroadphaseHelper.AddToChunk(chunk, entityId, aabb, isStatic, layer);
                        
                        newChunks.Add(chunk);
                    }
                }

                foreach (SAPChunk chunk in chunks)
                {
                    if (chunk == null)
                        continue;

                    BroadphaseHelper.RemoveFormChunk(chunk, entityId);
                }

                brRef.Chunks = newChunks;
                brRef.ChunksHash = chunksHash;
            }
        }
    }
}