using MiniEcs.Core;
using Unity.Mathematics;

namespace Models.Systems
{
    [EcsUpdateAfter(typeof(ResolveCollisionsSystem))]
    public class RaytracingSystem : IEcsSystem
    {
        private readonly CollisionMatrix _collisionMatrix;
        private static readonly IRayIntersectionCallback[] Intersections =
        {
            new RayIntersectionCircle(), new RayIntersectionRect()
        };

        private readonly EcsFilter _rayFilter;
        private readonly EcsFilter _targetsFilter;

        public RaytracingSystem(CollisionMatrix collisionMatrix)
        {
            _collisionMatrix = collisionMatrix;
            _rayFilter = new EcsFilter().AllOf(ComponentType.Ray, ComponentType.Translation, ComponentType.Rotation);
            _targetsFilter = new EcsFilter().AllOf(ComponentType.Translation, ComponentType.Rotation,
                ComponentType.RigBody, ComponentType.Collider);
        }

        public void Update(float deltaTime, EcsWorld world)
        {
            BroadphaseSAPComponent bpChunks =
                world.GetOrCreateSingleton<BroadphaseSAPComponent>(ComponentType.BroadphaseSAP); 

            IEcsGroup targetEntities = world.Filter(_targetsFilter);
            
            foreach (EcsEntity entity in world.Filter(_rayFilter))
            {
                TranslationComponent translation = (TranslationComponent) entity[ComponentType.Translation];
                RotationComponent rotation = (RotationComponent) entity[ComponentType.Rotation];
                
                RayComponent ray = (RayComponent) entity[ComponentType.Ray];
                ray.Hit = false;
                ray.Source = translation.Value;
                ray.Rotation = rotation.Value;

                float minDist = float.MaxValue;

                RayTrace(ray, out int[] chunks, out float2[] points);

                for (int i = 0; i < points.Length - 1; i++)
                {
                    SAPChunk chunk = BroadphaseHelper.GetOrCreateChunk(chunks[i], bpChunks);
                    float2 p1 = points[i];
                    float2 p2 = points[i + 1];

                    AABB segAABB = new AABB(
                        new float2(math.min(p1.x, p2.x), math.min(p1.y, p2.y)),
                        new float2(math.max(p1.x, p2.x), math.max(p1.y, p2.y)));

                    for (int j = 0; j < chunk.Length; j++)
                    {
                        BroadphaseAABB item = chunk.Items[j];

                        if (!item.AABB.Overlap(segAABB))
                            continue;

                        if ((_collisionMatrix.Data[ray.Layer] & item.Layer) != item.Layer &&
                            (_collisionMatrix.Data[item.Layer] & ray.Layer) != ray.Layer)
                            continue;

                        EcsEntity targetEntity = targetEntities[item.Id];
                        if (entity == targetEntity)
                            continue;

                        translation = (TranslationComponent) targetEntity[ComponentType.Translation];
                        rotation = (RotationComponent) targetEntity[ComponentType.Rotation];
                        ColliderComponent collider = (ColliderComponent) targetEntity[ComponentType.Collider];

                        int colliderType = (int) collider.ColliderType;
                        if (!Intersections[colliderType]
                            .HandleIntersection(ray, collider, translation, rotation, out float2 point))
                            continue;

                        float dist = math.distancesq(p1, point);
                        if (!(dist < minDist))
                            continue;

                        minDist = dist;
                        ray.HitPoint = point;
                    }

                    if (!(minDist < float.MaxValue)) 
                        continue;
                    
                    ray.Hit = true;
                    break;
                }
            }
        }

        private static void RayTrace(RayComponent rayComponent, out int[] chunks, out float2[] points)
        {
            const float cellSize = BroadphaseHelper.CellSize;
            const float offset = ushort.MaxValue * cellSize;
            
            float2 source = rayComponent.Source + offset;
            float2 target = rayComponent.Target + offset;

            float x0 = source.x / cellSize;
            float y0 = source.y / cellSize;
            float x1 = target.x / cellSize;
            float y1 = target.y / cellSize;
            float dx = math.abs(x1 - x0);
            float dy = math.abs(y1 - y0);

            int x = (int) math.abs(x0);
            int y = (int) math.abs(y0);

            float dtDx = 1.0f / dx;
            float dtDy = 1.0f / dy;
            float t = 0.0f;

            int n = 1;
            int xInc, yInc;
            float tnv, tnh;

            if (math.abs(dx) < MathHelper.EPSILON)
            {
                xInc = 0;
                tnh = dtDx;
            }
            else if (x1 > x0)
            {
                xInc = 1;
                n += (int) math.floor(x1) - x;
                tnh = (math.floor(x0) + 1 - x0) * dtDx;
            }
            else
            {
                xInc = -1;
                n += x - (int) math.floor(x1);
                tnh = (x0 - math.floor(x0)) * dtDx;
            }

            if (math.abs(dy) < MathHelper.EPSILON)
            {
                yInc = 0;
                tnv = dtDy;
            }
            else if (y1 > y0)
            {
                yInc = 1;
                n += (int) math.floor(y1) - y;
                tnv = (math.floor(y0) + 1 - y0) * dtDy;
            }
            else
            {
                yInc = -1;
                n += y - (int) math.floor(y1);
                tnv = (y0 - math.floor(y0)) * dtDy;
            }

            chunks = new int[n];
            points = new float2[n + 1];

            for (int i = 0; n > 0; n--, i++)
            {
                float xPos = t * (x1 - x0) * cellSize;
                float yPos = t * (y1 - y0) * cellSize;
                float2 pos = new float2(xPos, yPos);
                points[i] = source + pos - offset;

                short xChunk = (short) (x - ushort.MaxValue);
                short yChunk = (short) (y - ushort.MaxValue);
                chunks[i] = (xChunk << 16) | (ushort) yChunk;

                if (tnv < tnh)
                {
                    y += yInc;
                    t = tnv;
                    tnv += dtDy;
                }
                else
                {
                    x += xInc;
                    t = tnh;
                    tnh += dtDx;
                }
            }
            points[points.Length - 1] = target - offset;
        }
    }
}