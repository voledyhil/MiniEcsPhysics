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

            // Calculate translational vector, which is normal
            float2 normal = bTransform.Position - aTransform.Position;

            float distSqr = math.lengthsq(normal);
            float radius = a.Radius + b.Radius;

            if (distSqr >= radius * radius)
            {
                contactInfo.ContactCount = 0;
                return;
            }

            float distance = (float) Math.Sqrt(distSqr);
            contactInfo.ContactCount = 1;

            if (MathHelper.Equal(distance, 0.0f))
            {
                contactInfo.Penetration = a.Radius;
                contactInfo.Normal = new float2(1.0f, 0.0f);
                contactInfo.Contacts[0] = aTransform.Position;
            }
            else
            {
                contactInfo.Penetration = radius - distance;
                contactInfo.Normal = normal / distance;
                contactInfo.Contacts[0] = contactInfo.Normal * a.Radius + aTransform.Position;
            }
        }
    }
    
}