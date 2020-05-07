using MiniEcs.Components;
using MiniEcs.Core;
using MiniEcs.Core.Systems;
using Unity.Mathematics;

namespace Physics
{
    [EcsUpdateInGroup(typeof(PhysicsSystemGroup))]
    [EcsUpdateAfter(typeof(ResolveCollisionsSystem))]
    public class RaytracingSystem : IEcsSystem
    {
        private readonly CollisionMatrix _collisionMatrix;
        private readonly EcsFilter _rayFilter;
        private readonly EcsFilter _targetsFilter;

        public RaytracingSystem(CollisionMatrix collisionMatrix)
        {
            _collisionMatrix = collisionMatrix;
            _rayFilter = new EcsFilter().AllOf(ComponentType.Ray, ComponentType.Transform);
            _targetsFilter =
                new EcsFilter().AllOf(ComponentType.Transform, ComponentType.RigBody, ComponentType.Collider);
        }

        private int[] _chunksBuffer = new int[100];
        private float2[] _pointsBuffer = new float2[100];

        public unsafe void Update(float deltaTime, EcsWorld world)
        {
            BroadphaseSAPComponent bpChunks =
                world.GetOrCreateSingleton<BroadphaseSAPComponent>(ComponentType.BroadphaseSAP);

            IEcsGroup targetEntities = world.Filter(_targetsFilter);
            
            foreach (EcsEntity entity in world.Filter(_rayFilter))
            {
                TransformComponent tr = (TransformComponent) entity[ComponentType.Transform];
                RayComponent ray = (RayComponent) entity[ComponentType.Ray];

                ray.Hit = false;
                ray.Source = tr.Position;
                ray.Rotation = tr.Rotation;

                float minDist = float.MaxValue;
                RayTrace(ray, ref _chunksBuffer, ref _pointsBuffer, out int length);

                for (int i = 0; i < length - 1; i++)
                {
                    SAPChunk chunk = BroadphaseHelper.GetOrCreateChunk(_chunksBuffer[i], bpChunks);
                    float2 p1 = _pointsBuffer[i];
                    float2 p2 = _pointsBuffer[i + 1];

                    AABB sAABB = new AABB
                    {
                        Min = new float2(math.min(p1.x, p2.x), math.min(p1.y, p2.y)),
                        Max = new float2(math.max(p1.x, p2.x), math.max(p1.y, p2.y))
                    };

                    for (int j = 0; j < chunk.Length; j++)
                    {
                        BroadphaseAABB item = chunk.Items[j];
                        if (!item.AABB->Overlap(sAABB))
                            continue;

                        if (!_collisionMatrix.Check(ray.Layer, item.Layer))
                            continue;

                        EcsEntity targetEntity = targetEntities[item.Id];
                        if (entity == targetEntity)
                            continue;

                        tr = (TransformComponent) targetEntity[ComponentType.Transform];
                        ColliderComponent col = (ColliderComponent) targetEntity[ComponentType.Collider];

                        float2 hitPoint = float2.zero;
                        switch (col.ColliderType)
                        {
                            case ColliderType.Circle
                                when !OnIntersection(ray, (CircleColliderComponent) col, tr, out hitPoint):
                            case ColliderType.Rect
                                when !OnIntersection(ray, (RectColliderComponent) col, tr, out hitPoint):
                                continue;
                        }

                        float dist = math.distancesq(p1, hitPoint);
                        if (!(dist < minDist))
                            continue;

                        minDist = dist;
                        ray.HitPoint = hitPoint;
                    }

                    if (!(minDist < float.MaxValue))
                        continue;

                    ray.Hit = true;
                    break;
                }
            }
        }

        private static void RayTrace(RayComponent ray, ref int[] chunks, ref float2[] points, out int length)
        {
            const float cellSize = BroadphaseHelper.ChunkSize;
            const float offset = ushort.MaxValue * cellSize;

            float2 source = ray.Source + offset;
            float2 target = ray.Target + offset;

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

            length = n + 1;
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

            points[length - 1] = target - offset;
        }

        private static bool OnIntersection(RayComponent ray, CircleColliderComponent col, TransformComponent tr,
            out float2 hitPoint)
        {
            hitPoint = float2.zero;
            float2 source = ray.Source;
            float2 target = ray.Target;
            float2 pos = tr.Position;
            float r = col.Radius;

            float t;
            float dx = target.x - source.x;
            float dy = target.y - source.y;

            float a = dx * dx + dy * dy;
            float spDx = source.x - pos.x;
            float spDy = source.y - pos.y;
            float b = 2 * (dx * spDx + dy * spDy);
            float c = spDx * spDx + spDy * spDy - r * r;

            float det = b * b - 4 * a * c;
            if (a <= MathHelper.EPSILON || det < 0)
                return false;

            if (MathHelper.Equal(det, 0))
            {
                t = -b / (2 * a);
                hitPoint = new float2(source.x + t * dx, source.y + t * dy);
                return true;
            }

            float sqrtDet = math.sqrt(det);

            t = (-b + sqrtDet) / (2 * a);
            float2 p1 = new float2(source.x + t * dx, source.y + t * dy);

            t = (-b - sqrtDet) / (2 * a);
            float2 p2 = new float2(source.x + t * dx, source.y + t * dy);

            hitPoint = math.distancesq(ray.Source, p1) < math.distancesq(ray.Source, p2) ? p1 : p2;
            return true;
        }

        private static bool OnIntersection(RayComponent ray, RectColliderComponent col, TransformComponent tr,
            out float2 hitPoint)
        {
            hitPoint = float2.zero;
            float minDist = float.MaxValue;

            float2x2 rotate = float2x2.Rotate(tr.Rotation);
            float2x4 vertices = float2x4.zero;
            for (int i = 0; i < 4; i++)
                vertices[i] = MathHelper.Mul(rotate, col.Vertices[i]) + tr.Position;

            for (int i = 0; i < 4; i++)
            {
                int j = i + 1;
                if (j == 4)
                    j = 0;

                float2 p1 = vertices[i];
                float2 p2 = vertices[j];

                float2 b = ray.Target - ray.Source;
                float2 d = p2 - p1;

                float cross = b.x * d.y - b.y * d.x;
                if (MathHelper.Equal(cross, 0))
                    continue;

                float2 c = p1 - ray.Source;
                float t = (c.x * d.y - c.y * d.x) / cross;
                if (t < 0 || t > 1)
                    continue;

                float u = (c.x * b.y - c.y * b.x) / cross;
                if (u < 0 || u > 1)
                    continue;

                float2 p = ray.Source + t * b;

                float dist = math.distancesq(ray.Source, p);
                if (!(dist < minDist))
                    continue;

                minDist = dist;
                hitPoint = p;
            }

            return minDist < float.MaxValue;
        }
    }
}