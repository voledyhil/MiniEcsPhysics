using Unity.Mathematics;

namespace Models
{
	public class CollisionCircleRect : ICollisionCallback
	{
		public virtual void HandleCollision(ColliderComponent aCollider, TransformComponent aTransform, ColliderComponent bCollider,
			TransformComponent bTransform, out ContactInfo contactInfo)
		{
			contactInfo = new ContactInfo();
			
			CircleColliderComponent a = (CircleColliderComponent) aCollider;
			RectColliderComponent b = (RectColliderComponent) bCollider;

			contactInfo.Hit = false;

			float2x2 rotate = float2x2.Rotate(bTransform.Rotation);
			// Transform circle center to Polygon model space
			float2 center = MathHelper.Mul(MathHelper.Transpose(rotate), aTransform.Position - bTransform.Position);

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
				contactInfo.Hit = true;
				contactInfo.Normal = -(MathHelper.Mul(rotate, b.Normals[faceNormal]));
				contactInfo.HitPoint = contactInfo.Normal * a.Radius + aTransform.Position;
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

				contactInfo.Hit = true;
				float2 n = v1 - center;
				n = math.normalizesafe(MathHelper.Mul(rotate, n));
				contactInfo.Normal = n;
				v1 = MathHelper.Mul(rotate, v1) + bTransform.Position;
				contactInfo.HitPoint = v1;
			}

			else if (dot2 <= 0.0f)
			{
				if (math.distancesq(center, v2) > a.Radius * a.Radius)
					return;

				contactInfo.Hit = true;
				float2 n = v2 - center;
				v2 = MathHelper.Mul(rotate, v2) + bTransform.Position;
				contactInfo.HitPoint = v2;
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
				contactInfo.HitPoint = contactInfo.Normal * a.Radius + aTransform.Position;
				contactInfo.Hit = true;
			}
		}
	}
}