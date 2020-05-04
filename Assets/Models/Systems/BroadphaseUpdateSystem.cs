using System;
using System.Collections.Generic;
using MiniEcs.Core;

namespace Models.Systems
{
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
                TranslationComponent translation = (TranslationComponent) entity[ComponentType.Translation];
                RotationComponent rotation = (RotationComponent) entity[ComponentType.Rotation];
                ColliderComponent collider = (ColliderComponent) entity[ComponentType.Collider];
                RigBodyComponent rigBody = (RigBodyComponent) entity[ComponentType.RigBody];
                BroadphaseRefComponent broadphaseRef = (BroadphaseRefComponent) entity[ComponentType.BroadphaseRef];

                AABB aabb = new AABB(collider.Size, translation.Value,
                    collider.ColliderType == ColliderType.Rect ? rotation.Value : 0f);


                List<SAPChunk> oldChunks = broadphaseRef.Items;
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
                            IsStatic = MathHelper.Equal(rigBody.InvMass, 0),
                            Layer = collider.Layer
                        };
                    }
                }

                foreach (SAPChunk chunk in oldChunks)
                {
                    if (chunk == null)
                        continue;

                    for (int i = 0; i < chunk.Length; i++)
                    {
                        BroadphaseAABB item = chunk.Items[i];
                        if (item.Id != entity.Id)
                            continue;

                        chunk.NeedRebuild = true;
                        chunk.IsDirty = true;
                        chunk.Items[i].Id = uint.MaxValue;
                        break;
                    }
                }

                broadphaseRef.Items = newChunks;
            }
        }
    }
}