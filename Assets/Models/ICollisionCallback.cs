namespace Models
{
    public interface ICollisionCallback
    {
        void HandleCollision(ColliderComponent aCollider, TransformComponent aTransform, ColliderComponent bCollider, TransformComponent bTransform, out ContactInfo contactInfo);
    }
}