using System;
using Unity.Mathematics;

namespace Models
{
    public class CollisionCircleCircle : ICollisionCallback
    {
        public void HandleCollision(ColliderComponent aCollider, TranslationComponent aTranslation, RotationComponent aRotation, ColliderComponent bCollider,
            TranslationComponent bTranslation, RotationComponent bRotation, out ContactInfo contactInfo)
        {
            contactInfo = new ContactInfo();
            CircleColliderComponent a = (CircleColliderComponent) aCollider;
            CircleColliderComponent b = (CircleColliderComponent) bCollider;

            // Calculate translational vector, which is normal
            float2 normal = bTranslation.Value - aTranslation.Value;

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
                contactInfo.Contacts[0] = aTranslation.Value;
            }
            else
            {
                contactInfo.Penetration = radius - distance;
                contactInfo.Normal = normal / distance;
                contactInfo.Contacts[0] = contactInfo.Normal * a.Radius + aTranslation.Value;
            }
        }
    }
    
}