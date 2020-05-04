using Unity.Mathematics;

namespace Models
{
    public class RayIntersectionCircle : IRayIntersectionCallback
    {
        public bool HandleIntersection(RayComponent ray, ColliderComponent collider, TranslationComponent translation, RotationComponent rotation,
            out float2 hitPoint)
        {
            hitPoint = float2.zero;
            float2 source = ray.Source;
            float2 target = ray.Target;

            CircleColliderComponent circleCollider = (CircleColliderComponent) collider;
            float2 pos = translation.Value;
            float r = circleCollider.Radius;

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
    }
}