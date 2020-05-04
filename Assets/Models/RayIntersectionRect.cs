using Unity.Mathematics;

namespace Models
{
    public class RayIntersectionRect : IRayIntersectionCallback
    {
        public bool HandleIntersection(RayComponent ray, ColliderComponent collider, TranslationComponent translation, RotationComponent rotation, out float2 hitPoint)
        {
            RectColliderComponent rectCollider = (RectColliderComponent) collider;

            hitPoint = float2.zero;
            float minDist = float.MaxValue;

            float2x2 rotate = float2x2.Rotate(rotation.Value);
            float2x4 vertices = float2x4.zero;
            for (int i = 0; i < 4; i++)
                vertices[i] = MathHelper.Mul(rotate, rectCollider.Vertices[i]) + translation.Value;
            
            for (int i = 0; i < 4; i++)
            {
                int j = i + 1;
                if (j == 4)
                    j = 0;

                float2 p1 = vertices[i];
                float2 p2 = vertices[j];
                
                float2 b = ray.Target - ray.Source;
                float2 d = p2 - p1;

                float cross = MathHelper.Cross(b, d);
                if (MathHelper.Equal(cross, 0))
                    continue;

                float2 c = p1 - ray.Source;
                float t = MathHelper.Cross(c, d) / cross;
                if (t < 0 || t > 1)
                    continue;

                float u = MathHelper.Cross(c, b) / cross;
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