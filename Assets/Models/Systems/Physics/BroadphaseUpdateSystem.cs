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
            _entitiesFilter = new EcsFilter().AllOf(ComponentType.Translation, ComponentType.Rotation,
                ComponentType.Collider, ComponentType.RigBody, ComponentType.BroadphaseRef).NoneOf(ComponentType.RigBodyStatic);
        }

        public void Update(float deltaTime, EcsWorld world)
        {
            BroadphaseSAPComponent bpChunks =
                world.GetOrCreateSingleton<BroadphaseSAPComponent>(ComponentType.BroadphaseSAP); 

            foreach (EcsEntity entity in world.Filter(_entitiesFilter))
            {
                TranslationComponent tr = (TranslationComponent) entity[ComponentType.Translation];
                RotationComponent rot = (RotationComponent) entity[ComponentType.Rotation];
                ColliderComponent col = (ColliderComponent) entity[ComponentType.Collider];
                RigBodyComponent rig = (RigBodyComponent) entity[ComponentType.RigBody];
                BroadphaseRefComponent brRef = (BroadphaseRefComponent) entity[ComponentType.BroadphaseRef];

                AABB aabb = new AABB(col.Size, tr.Value, col.ColliderType == ColliderType.Rect ? rot.Value : 0f);

                List<SAPChunk> oldChunks = brRef.Items;
                List<SAPChunk> newChunks = new List<SAPChunk>(BroadphaseHelper.GetChunks(aabb, bpChunks));

                foreach (SAPChunk chunk in newChunks)
                {
                    chunk.IsDirty = true;

                    int index = oldChunks.IndexOf(chunk);
                    if (index >= 0)
                    {
                        oldChunks[index] = null;
                        
                        for (int i = 0; i < chunk.Length; i++)
                        {
                            if (chunk.Items[i].Id != entity.Id)
                                continue;
                            chunk.Items[i].AABB = aabb;
                            chunk.IsDirty = true;
                            break;
                        }
                    }
                    else
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
                    }
                }

                foreach (SAPChunk chunk in oldChunks)
                {
                    if (chunk == null)
                        continue;

                    BroadphaseHelper.RemoveFormChunk(chunk, entity.Id);
                }

                brRef.Items = newChunks;
            }
        }
    }
}