using System;
using System.Collections.Generic;
using System.Linq;
using MiniEcs.Core;
using MiniEcs.Core.Systems;

namespace Models.Systems.Physics
{
    [EcsUpdateInGroup(typeof(PhysicsSystemGroup))]
    [EcsUpdateBefore(typeof(IntegrateVelocitySystem))]
    public class BroadphaseInitSystem : IEcsSystem
    {
        private readonly EcsFilter _entitiesFilter;
        
        public BroadphaseInitSystem()
        {
            _entitiesFilter = new EcsFilter()
                .AllOf(ComponentType.Transform, ComponentType.Collider, ComponentType.RigBody)
                .NoneOf(ComponentType.BroadphaseRef);
        }
        
        public unsafe void Update(float deltaTime, EcsWorld world)
        {
            BroadphaseSAPComponent bpChunks =
                world.GetOrCreateSingleton<BroadphaseSAPComponent>(ComponentType.BroadphaseSAP); 
            
            List<EcsEntity> entities = world.Filter(_entitiesFilter).ToList();

            foreach (EcsEntity entity in entities)
            {
                uint entityId = entity.Id;

                TransformComponent tr = (TransformComponent) entity[ComponentType.Transform];
                ColliderComponent col = (ColliderComponent) entity[ComponentType.Collider];
                RigBodyComponent rig = (RigBodyComponent) entity[ComponentType.RigBody];

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
                entity[ComponentType.BroadphaseRef] = bpRef;

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
                            Layer = layer
                        };
                    }

                    if (!isStatic)
                        chunk.DynamicCounter++;
                }
            }
        }
    }
}