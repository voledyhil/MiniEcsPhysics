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
                TranslationComponent tr = (TranslationComponent) entity[ComponentType.Translation];
                RotationComponent rot = (RotationComponent) entity[ComponentType.Rotation];
                ColliderComponent col = (ColliderComponent) entity[ComponentType.Collider];
                RigBodyComponent rig = (RigBodyComponent) entity[ComponentType.RigBody];

                AABB aabb = new AABB(col.Size, tr.Value, col.ColliderType == ColliderType.Rect ? rot.Value : 0f);

                List<SAPChunk> chunks = new List<SAPChunk>();
                foreach (SAPChunk chunk in BroadphaseHelper.GetChunks(aabb, bpChunks))
                {
                    if (chunk.Length >= chunk.Items.Length)
                        Array.Resize(ref chunk.Items, 2 * chunk.Length);

                    chunk.Items[chunk.Length++] = new BroadphaseAABB
                    {
                        AABB = aabb, 
                        Id = entity.Id, 
                        IsStatic = MathHelper.Equal(rig.InvMass, 0),
                        Layer = col.Layer
                    };
                    chunk.IsDirty = true;

                    chunks.Add(chunk);
                }

                BroadphaseRefComponent bpRef = new BroadphaseRefComponent {Items = chunks};
                entity[ComponentType.BroadphaseRef] = bpRef;
            }
        }
    }
}