using Unity.Mathematics;

namespace Models
{
    public interface IRayIntersectionCallback
    {
        bool HandleIntersection(RayComponent ray, ColliderComponent collider, TransformComponent transform, out float2 hitPoint);
    }
}