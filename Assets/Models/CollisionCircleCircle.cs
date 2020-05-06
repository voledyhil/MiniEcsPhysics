using System;
using Unity.Mathematics;

namespace Models
{
    public class CollisionCircleCircle : ICollisionCallback
    {
        public void HandleCollision(ColliderComponent aCollider, TransformComponent aTransform, ColliderComponent bCollider,
            TransformComponent bTransform, out ContactInfo contactInfo)
        {
            contactInfo = new ContactInfo();
            CircleColliderComponent a = (CircleColliderComponent) aCollider;
            CircleColliderComponent b = (CircleColliderComponent) bCollider;

            float2 normal = bTransform.Position - aTransform.Position;
            float distSqr = math.lengthsq(normal);
            float radius = a.Radius + b.Radius;

            if (distSqr >= radius * radius)
            {
                contactInfo.Hit = false;
                return;
            }

            float distance = (float) Math.Sqrt(distSqr);
            contactInfo.Hit = true;

            if (MathHelper.Equal(distance, 0.0f))
            {
                contactInfo.Penetration = a.Radius;
                contactInfo.Normal = new float2(1.0f, 0.0f);
                contactInfo.HitPoint = aTransform.Position;
            }
            else
            {
                contactInfo.Penetration = radius - distance;
                contactInfo.Normal = normal / distance;
                contactInfo.HitPoint = contactInfo.Normal * a.Radius + aTransform.Position;
            }
        }
    }
    
}