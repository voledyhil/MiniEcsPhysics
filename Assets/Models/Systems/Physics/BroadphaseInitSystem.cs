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
                .AllOf(ComponentType.Translation, ComponentType.Rotation, ComponentType.Collider, ComponentType.RigBody)
                .NoneOf(ComponentType.BroadphaseRef);
        }
        
        public void Update(float deltaTime, EcsWorld world)
        {
            BroadphaseSAPComponent bpChunks =
                world.GetOrCreateSingleton<BroadphaseSAPComponent>(ComponentType.BroadphaseSAP); 
            
            List<EcsEntity> entities = world.Filter(_entitiesFilter).ToList();

            foreach (EcsEntity entity in entities)
            {
                uint entityId = entity.Id;
                
                TranslationComponent tr = (TranslationComponent) entity[ComponentType.Translation];
                RotationComponent rot = (RotationComponent) entity[ComponentType.Rotation];
                ColliderComponent col = (ColliderComponent) entity[ComponentType.Collider];
                RigBodyComponent rig = (RigBodyComponent) entity[ComponentType.RigBody];

                AABB aabb = new AABB(col.Size, tr.Value, col.ColliderType == ColliderType.Rect ? rot.Value : 0f);
                bool isStatic = MathHelper.Equal(rig.InvMass, 0);
                int layer = col.Layer;
                
                List<SAPChunk> chunks = new List<SAPChunk>(4);
                foreach (int chunkId in BroadphaseHelper.GetChunks(aabb))
                {
                    SAPChunk chunk = BroadphaseHelper.GetOrCreateChunk(chunkId, bpChunks);

                    BroadphaseHelper.AddToChunk(chunk, entityId, aabb, isStatic, layer);

                    chunks.Add(chunk);
                }

                BroadphaseRefComponent bpRef = new BroadphaseRefComponent
                {
                    Chunks = chunks,
                    ChunksHash = BroadphaseHelper.CalculateChunksHash(aabb)
                };
                entity[ComponentType.BroadphaseRef] = bpRef;
            }
        }
    }
}