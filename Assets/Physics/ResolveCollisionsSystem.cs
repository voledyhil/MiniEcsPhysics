using System;
using MiniEcs.Components;
using MiniEcs.Core;
using MiniEcs.Core.Systems;
using Unity.Mathematics;

namespace Physics
{
	[EcsUpdateInGroup(typeof(PhysicsSystemGroup))]
	[EcsUpdateAfter(typeof(BroadphaseCalculatePairSystem))]
	public class ResolveCollisionsSystem : IEcsSystem
	{
		public void Update(float deltaTime, EcsWorld world)
		{
			BroadphaseSAPComponent bpChunks =
				world.GetOrCreateSingleton<BroadphaseSAPComponent>(ComponentType.BroadphaseSAP);

			foreach (BroadphasePair pair in bpChunks.Pairs)
			{
				EcsEntity entityA = pair.EntityA;
				EcsEntity entityB = pair.EntityB;

				TransformComponent ta = (TransformComponent) entityA[ComponentType.Transform];
				TransformComponent tb = (TransformComponent) entityB[ComponentType.Transform];
				ColliderComponent ca = (ColliderComponent) entityA[ComponentType.Collider];
				ColliderComponent cb = (ColliderComponent) entityB[ComponentType.Collider];

				ContactInfo info;
				switch (ca.ColliderType)
				{
					case ColliderType.Circle when cb.ColliderType == ColliderType.Circle:
						OnCollision((CircleColliderComponent) ca, ta, (CircleColliderComponent) cb, tb, out info);
						break;
					case ColliderType.Circle when cb.ColliderType == ColliderType.Rect:
						OnCollision((CircleColliderComponent) ca, ta, (RectColliderComponent) cb, tb, out info);
						break;
					case ColliderType.Rect when cb.ColliderType == ColliderType.Circle:
						OnCollision((CircleColliderComponent) cb, tb, (RectColliderComponent) ca, ta, out info);
						info.Normal = -info.Normal;
						break;
					default:
						throw new InvalidOperationException();
				}

				if (!info.Hit)
					continue;
				
				RigBodyComponent ra = (RigBodyComponent) entityA[ComponentType.RigBody];
				RigBodyComponent rb = (RigBodyComponent) entityB[ComponentType.RigBody];

				float2 rv = rb.Velocity - ra.Velocity;

				float contactVel = math.dot(rv, info.Normal);
				float invMassSum = ra.InvMass + rb.InvMass;

				float f = -contactVel / invMassSum;
				float2 impulse = info.Normal * f * deltaTime;

				ra.Velocity -= ra.InvMass * impulse;
				rb.Velocity += rb.InvMass * impulse;

				float2 correction = info.Penetration / (ra.InvMass + rb.InvMass) * info.Normal * 0.5f;
				ta.Position -= correction * ra.InvMass;
				tb.Position += correction * rb.InvMass;
			}
		}

		private struct ContactInfo
		{
			public float Penetration;
			public float2 Normal;
			public float2 HitPoint;
			public bool Hit;
		}

		private static void OnCollision(CircleColliderComponent ca, TransformComponent ta,
			RectColliderComponent cb, TransformComponent tb, out ContactInfo contactInfo)
		{
			contactInfo = new ContactInfo {Hit = false};

			float2x2 rotate = float2x2.Rotate(tb.Rotation);
			float2 center = MathHelper.Mul(MathHelper.Transpose(rotate), ta.Position - tb.Position);

			float separation = float.MinValue;
			int faceNormal = 0;
			for (int i = 0; i < 4; ++i)
			{
				float s = math.dot(cb.Normals[i], center - cb.Vertices[i]);
				if (s > ca.Radius)
					return;

				if (!(s > separation))
					continue;

				separation = s;
				faceNormal = i;
			}

			if (separation < MathHelper.EPSILON)
			{
				contactInfo.Hit = true;
				contactInfo.Normal = -MathHelper.Mul(rotate, cb.Normals[faceNormal]);
				contactInfo.HitPoint = contactInfo.Normal * ca.Radius + ta.Position;
				contactInfo.Penetration = ca.Radius;
				return;
			}
			
			contactInfo.Penetration = ca.Radius - separation;

			float2 v1 = cb.Vertices[faceNormal];
			float2 v2 = cb.Vertices[faceNormal + 1 < 4 ? faceNormal + 1 : 0];

			if (math.dot(center - v1, v2 - v1) <= 0.0f)
			{
				if (math.distancesq(center, v1) > ca.Radius * ca.Radius)
					return;

				contactInfo.Hit = true;
				contactInfo.Normal = math.normalizesafe(MathHelper.Mul(rotate, v1 - center));
				contactInfo.HitPoint = MathHelper.Mul(rotate, v1) + tb.Position;
				return;
			}
			
			if (math.dot(center - v2, v1 - v2) <= 0.0f)
			{
				if (math.distancesq(center, v2) > ca.Radius * ca.Radius)
					return;

				contactInfo.Hit = true;
				contactInfo.Normal = math.normalizesafe(MathHelper.Mul(rotate, v2 - center));
				contactInfo.HitPoint = MathHelper.Mul(rotate, v2) + tb.Position;
				return;
			}

			float2 n = cb.Normals[faceNormal];
			if (math.dot(center - v1, n) > ca.Radius)
				return;

			contactInfo.Normal = -MathHelper.Mul(rotate, n);
			contactInfo.HitPoint = contactInfo.Normal * ca.Radius + ta.Position;
			contactInfo.Hit = true;
		}

		private static void OnCollision(CircleColliderComponent ca, TransformComponent ta, CircleColliderComponent cb,
			TransformComponent tb, out ContactInfo contactInfo)
		{
			contactInfo = new ContactInfo();
			float2 normal = tb.Position - ta.Position;
			float distSqr = math.lengthsq(normal);
			float radius = ca.Radius + cb.Radius;

			if (distSqr >= radius * radius)
			{
				contactInfo.Hit = false;
				return;
			}

			float distance = math.sqrt(distSqr);
			contactInfo.Hit = true;

			if (MathHelper.Equal(distance, 0.0f))
			{
				contactInfo.Penetration = ca.Radius;
				contactInfo.Normal = new float2(1.0f, 0.0f);
				contactInfo.HitPoint = ta.Position;
			}
			else
			{
				contactInfo.Penetration = radius - distance;
				contactInfo.Normal = normal / distance;
				contactInfo.HitPoint = contactInfo.Normal * ca.Radius + ta.Position;
			}
		}
	}
}