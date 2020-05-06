namespace Models
{
    public class CollisionRectCircle : CollisionCircleRect
    {
        public override void HandleCollision(ColliderComponent aCollider, TransformComponent aTransform, ColliderComponent bCollider,
            TransformComponent bTransform, out ContactInfo contactInfo)
        {
            base.HandleCollision(bCollider, bTransform, aCollider, aTransform, out contactInfo);

            if (!contactInfo.Hit) 
                return;
            
            contactInfo.Normal = -contactInfo.Normal;
        }
    }
}