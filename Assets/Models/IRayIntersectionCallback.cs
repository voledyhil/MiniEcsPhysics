using Unity.Mathematics;

namespace Models
{
    public interface IRayIntersectionCallback
    {
        bool HandleIntersection(RayComponent ray, ColliderComponent collider, TranslationComponent translation, RotationComponent rotation, out float2 hitPoint);
    }
}