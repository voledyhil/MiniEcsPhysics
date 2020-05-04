using Unity.Mathematics;

namespace Models
{
	public class CollisionCircleRect : ICollisionCallback
	{
		public virtual void HandleCollision(ColliderComponent aCollider, TranslationComponent aTranslation, RotationComponent aRotation, ColliderComponent bCollider,
			TranslationComponent bTranslation, RotationComponent bRotation, out ContactInfo contactInfo)
		{
			contactInfo = new ContactInfo();
			
			CircleColliderComponent a = (CircleColliderComponent) aCollider;
			RectColliderComponent b = (RectColliderComponent) bCollider;

			contactInfo.ContactCount = 0;

			float2x2 rotate = float2x2.Rotate(bRotation.Value);
			// Transform circle center to Polygon model space
			float2 center = MathHelper.Mul(MathHelper.Transpose(rotate), aTranslation.Value - bTranslation.Value);

			// Find edge with minimum penetration
			// Exact concept as using support points in Polygon vs Polygon
			float separation = float.MinValue;
			int faceNormal = 0;
			for (int i = 0; i < 4; ++i)
			{
				float s = math.dot(b.Normals[i], center - b.Vertices[i]);

				if (s > a.Radius)
				{
					return;
				}

				if (!(s > separation)) 
					continue;
				
				separation = s;
				faceNormal = i;
			}

			// Grab face's vertices
			float2 v1 = b.Vertices[faceNormal];
			int i2 = faceNormal + 1 < 4 ? faceNormal + 1 : 0;
			float2 v2 = b.Vertices[i2];

			// Check to see if center is within polygon
			if (separation < MathHelper.EPSILON)
			{
				contactInfo.ContactCount = 1;
				contactInfo.Normal = -(MathHelper.Mul(rotate, b.Normals[faceNormal]));
				contactInfo.Contacts[0] = contactInfo.Normal * a.Radius + aTranslation.Value;
				contactInfo.Penetration = a.Radius;

				return;
			}

			// Determine which voronoi region of the edge center of circle lies within
			float dot1 = math.dot(center - v1, v2 - v1);
			float dot2 = math.dot(center - v2, v1 - v2);
			contactInfo.Penetration = a.Radius - separation;

			if (dot1 <= 0.0f)
			{
				if (math.distancesq(center, v1) > a.Radius * a.Radius)
					return;

				contactInfo.ContactCount = 1;
				float2 n = v1 - center;
				n = math.normalizesafe(MathHelper.Mul(rotate, n));
				contactInfo.Normal = n;
				v1 = MathHelper.Mul(rotate, v1) + bTranslation.Value;
				contactInfo.Contacts[0] = v1;
			}

			else if (dot2 <= 0.0f)
			{
				if (math.distancesq(center, v2) > a.Radius * a.Radius)
					return;

				contactInfo.ContactCount = 1;
				float2 n = v2 - center;
				v2 = MathHelper.Mul(rotate, v2) + bTranslation.Value;
				contactInfo.Contacts[0] = v2;
				n = math.normalizesafe(MathHelper.Mul(rotate, n));
				contactInfo.Normal = n;
			}
			else
			{
				float2 n = b.Normals[faceNormal];

				if (math.dot(center - v1, n) > a.Radius)
					return;

				n = MathHelper.Mul(rotate, n);
				contactInfo.Normal = -n;
				contactInfo.Contacts[0] = contactInfo.Normal * a.Radius + aTranslation.Value;
				contactInfo.ContactCount = 1;
			}
		}
	}
}