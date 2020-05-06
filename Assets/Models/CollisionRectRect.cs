using Unity.Mathematics;

namespace Models
{
	public class CollisionRectRect : ICollisionCallback
	{
		public void HandleCollision(ColliderComponent aCollider, TransformComponent aTransform,
			ColliderComponent bCollider, TransformComponent bTransform, out ContactInfo contactInfo)
		{
			contactInfo = new ContactInfo();

			RectColliderComponent a = (RectColliderComponent) aCollider;
			RectColliderComponent b = (RectColliderComponent) bCollider;
			contactInfo.ContactCount = 0;

			float2x2 aRotate = float2x2.Rotate(aTransform.Rotation);
			float2x2 bRotate = float2x2.Rotate(bTransform.Rotation);

			float penetrationA = FindAxisLeastPenetration(a, aRotate, aTransform.Position, b, bRotate,
				bTransform.Position, out int faceA);
			if (penetrationA >= 0.0f)
			{
				return;
			}

			float penetrationB = FindAxisLeastPenetration(b, bRotate, bTransform.Position, a, aRotate,
				aTransform.Position, out int faceB);
			if (penetrationB >= 0.0f)
			{
				return;
			}

			int referenceIndex;
			bool flip;

			RectColliderComponent refPoly;
			float2x2 refRotate;
			float2 refPosition;

			RectColliderComponent incPoly;
			float2x2 incRotate;
			float2 incPosition;

			if (penetrationA >= penetrationB)
			{
				refPoly = a;
				refRotate = aRotate;
				refPosition = aTransform.Position;

				incPoly = b;
				incRotate = bRotate;
				incPosition = bTransform.Position;

				referenceIndex = faceA;
				flip = false;
			}
			else
			{
				refPoly = b;
				refRotate = bRotate;
				refPosition = bTransform.Position;

				incPoly = a;
				incRotate = aRotate;
				incPosition = aTransform.Position;

				referenceIndex = faceB;
				flip = true;
			}

			FindIncidentFace(refPoly, refRotate, incPoly, incRotate, incPosition, referenceIndex,
				out float2 incidentFace0, out float2 incidentFace1);

			// y
			// ^ .n ^
			// +---c ------posPlane--
			// x < | i |\
			// +---+ c-----negPlane--
			// \ v
			// r
			//
			// r : reference face
			// i : incident poly
			// c : clipped point
			// n : incident normal

			// Setup reference face vertices
			float2 v1 = refPoly.Vertices[referenceIndex];
			referenceIndex = referenceIndex + 1 == 4 ? 0 : referenceIndex + 1;
			float2 v2 = refPoly.Vertices[referenceIndex];


			// Transform vertices to world space
			v1 = MathHelper.Mul(refRotate, v1) + refPosition;
			v2 = MathHelper.Mul(refRotate, v2) + refPosition;

			// Calculate reference face side normal in world space
			float2 sidePlaneNormal = math.normalizesafe(v2 - v1);

			// Orthogonalize
			float2 refFaceNormal = new float2(sidePlaneNormal.y, -sidePlaneNormal.x);

			// ax + by = c
			// c is distance from origin
			float refC = math.dot(refFaceNormal, v1);

			// Flip
			contactInfo.Normal = refFaceNormal;
			if (flip)
			{
				contactInfo.Normal = -contactInfo.Normal;
			}

			// Keep points behind reference face
			int cp = 0; // clipped points behind reference face
			float separation = math.dot(refFaceNormal, incidentFace0) - refC;
			if (separation <= 0.0f)
			{
				contactInfo.Contacts[cp] = incidentFace0;
				contactInfo.Penetration = -separation;
				cp++;
			}
			else
			{
				contactInfo.Penetration = 0;
			}

			separation = math.dot(refFaceNormal, incidentFace1) - refC;

			if (separation <= 0.0f)
			{
				contactInfo.Contacts[cp] = incidentFace1;
				contactInfo.Penetration += -separation;
				cp++;

				// Average penetration
				contactInfo.Penetration /= cp;
			}

			contactInfo.ContactCount = cp;
		}

		private static float FindAxisLeastPenetration(RectColliderComponent a, float2x2 aRotate, float2 aPosition,
			RectColliderComponent b, float2x2 bRotate, float2 bPosition, out int faceIndex)
		{
			float bestDistance = float.MinValue;
			int bestIndex = 0;

			for (int i = 0; i < 4; ++i)
			{
				// Retrieve a face normal from A
				float2 n = a.Normals[i];
				float2 nw = MathHelper.Mul(aRotate, n);

				// Transform face normal into B's model space
				float2x2 buT = math.transpose(bRotate);
				n = MathHelper.Mul(buT, nw);

				// Retrieve support point from B along -n
				float2 s = GetSupport(b, -n);

				// Retrieve vertex on face from A, transform into
				// B's model space
				float2 v = a.Vertices[i];
				v = MathHelper.Mul(aRotate, v) + aPosition;
				v -= bPosition;
				v = MathHelper.Mul(buT, v);

				// Compute penetration distance (in B's model space)
				float d = math.dot(n, s - v);

				// Store greatest distance
				if (!(d > bestDistance))
					continue;

				bestDistance = d;
				bestIndex = i;
			}

			faceIndex = bestIndex;
			return bestDistance;
		}

		private static float2 GetSupport(RectColliderComponent rectCollider, float2 dir)
		{
			float bestProjection = float.MinValue;
			float2 bestVertex = float2.zero;

			for (int i = 0; i < 4; i++)
			{
				float2 vertex = rectCollider.Vertices[i];
				float projection = math.dot(vertex, dir);

				if (!(projection > bestProjection))
					continue;

				bestVertex = vertex;
				bestProjection = projection;
			}

			return bestVertex;
		}

		private static void FindIncidentFace(RectColliderComponent refPoly, float2x2 refRotate, RectColliderComponent incPoly,
			float2x2 incRotate, float2 incPosition, int referenceIndex, out float2 incidentFace0,
			out float2 incidentFace1)
		{
			float2 referenceNormal = refPoly.Normals[referenceIndex];

			// Calculate normal in incident's frame of reference
			// incident's model space
			referenceNormal = MathHelper.Mul(refRotate, referenceNormal); // To world space
			referenceNormal = MathHelper.Mul(math.transpose(incRotate), referenceNormal); // To
			// incident's
			// model
			// space

			// Find most anti-normal face on incident polygon
			int incidentFace = 0;
			float minDot = float.MaxValue;
			for (int i = 0; i < 4; ++i)
			{
				float dot = math.dot(referenceNormal, incPoly.Normals[i]);

				if (!(dot < minDot))
					continue;

				minDot = dot;
				incidentFace = i;
			}

			// Assign face vertices for incidentFace
			incidentFace0 = MathHelper.Mul(incRotate, incPoly.Vertices[incidentFace]) + incPosition;
			incidentFace = incidentFace + 1 >= 4 ? 0 : incidentFace + 1;
			incidentFace1 = MathHelper.Mul(incRotate, incPoly.Vertices[incidentFace]) + incPosition;
		}
	}
}