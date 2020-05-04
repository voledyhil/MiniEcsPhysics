namespace Models
{
    public class CollisionRectCircle : CollisionCircleRect
    {
        public override void HandleCollision(ColliderComponent aCollider, TranslationComponent aTranslation, RotationComponent aRotation, ColliderComponent bCollider,
            TranslationComponent bTranslation, RotationComponent bRotation, out ContactInfo contactInfo)
        {
            base.HandleCollision(bCollider, bTranslation, bRotation, aCollider, aTranslation, aRotation, out contactInfo);

            if (contactInfo.ContactCount <= 0) 
                return;
            
            contactInfo.Normal = -contactInfo.Normal;
        }
    }
}