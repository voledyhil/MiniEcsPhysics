namespace Models
{
    public interface ICollisionCallback
    {
        void HandleCollision(ColliderComponent aCollider, TranslationComponent aTranslation, RotationComponent aRotation, ColliderComponent bCollider, TranslationComponent bTranslation, RotationComponent bRotation, out ContactInfo contactInfo);
    }
}