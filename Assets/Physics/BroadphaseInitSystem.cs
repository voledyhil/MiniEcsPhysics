using System;
using System.Collections.Generic;
using System.Linq;
using MiniEcs.Core;
using MiniEcs.Core.Systems;

namespace Physics
{
    [EcsUpdateInGroup(typeof(PhysicsSystemGroup))]
    [EcsUpdateBefore(typeof(IntegrateVelocitySystem))]
    public class BroadphaseInitSystem : IEcsSystem
    {
        private readonly EcsFilter _entitiesFilter;
        
        public BroadphaseInitSystem()
        {
            _entitiesFilter = new EcsFilter().AllOf<TransformComponent, ColliderComponent, RigBodyComponent>().NoneOf<BroadphaseRefComponent>();
        }
        
        public unsafe void Update(float deltaTime, EcsWorld world)
        {
            BroadphaseSAPComponent bpChunks = world.GetOrCreateSingleton<BroadphaseSAPComponent>(); 
            
            List<IEcsEntity> entities = world.Filter(_entitiesFilter).ToList();

            foreach (IEcsEntity entity in entities)
            {
                uint entityId = entity.Id;

                TransformComponent tr = entity.GetComponent<TransformComponent>();
                ColliderComponent col = entity.GetComponent<ColliderComponent>();
                RigBodyComponent rig = entity.GetComponent<RigBodyComponent>();

                AABB aabb = new AABB(col.Size, tr.Position, col.ColliderType == ColliderType.Rect ? tr.Rotation : 0f);
                bool isStatic = MathHelper.Equal(rig.InvMass, 0);
                int layer = col.Layer;

                List<SAPChunk> chunks = new List<SAPChunk>(4);
                foreach (int chunkId in BroadphaseHelper.GetChunks(aabb))
                {
                    chunks.Add(BroadphaseHelper.GetOrCreateChunk(chunkId, bpChunks));
                }
                
                BroadphaseRefComponent bpRef = new BroadphaseRefComponent
                {
                    Chunks = chunks,
                    ChunksHash = BroadphaseHelper.CalculateChunksHash(aabb),
                    AABB = aabb
                };
                entity.AddComponent(bpRef);

                foreach (SAPChunk chunk in chunks)
                {
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
                }
            }
        }
    }
}