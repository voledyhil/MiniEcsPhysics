using MiniEcs.Core;
using MiniEcs.Core.Systems;
using Unity.Mathematics;

namespace Models.Systems.Physics
{
	[EcsUpdateInGroup(typeof(PhysicsSystemGroup))]
	[EcsUpdateAfter(typeof(BroadphaseCalculatePairSystem))]
	public class ResolveCollisionsSystem : IEcsSystem
	{
		private static readonly ICollisionCallback[,] Collisions =
		{
			{new CollisionCircleCircle(), new CollisionCircleRect()},
			{new CollisionRectCircle(), new CollisionRectRect()}
		};

		private readonly EcsFilter _entitiesFilter;

		public ResolveCollisionsSystem()
		{
			_entitiesFilter = new EcsFilter().AllOf(ComponentType.Translation, ComponentType.Rotation,
				ComponentType.RigBody, ComponentType.Collider);
		}

		public void Update(float deltaTime, EcsWorld world)
		{
			BroadphaseSAPComponent bpChunks =
				world.GetOrCreateSingleton<BroadphaseSAPComponent>(ComponentType.BroadphaseSAP);

			IEcsGroup entities = world.Filter(_entitiesFilter);

			foreach (long pair in bpChunks.Pairs)
			{
				uint i = (uint) (pair & uint.MaxValue);
				uint j = (uint) (pair >> 32);

				EcsEntity entityA = entities[i];
				EcsEntity entityB = entities[j];

				TranslationComponent trA = (TranslationComponent) entityA[ComponentType.Translation];
				RotationComponent rotA = (RotationComponent) entityA[ComponentType.Rotation];
				RigBodyComponent rigA = (RigBodyComponent) entityA[ComponentType.RigBody];
				ColliderComponent colA = (ColliderComponent) entityA[ComponentType.Collider];

				TranslationComponent trB = (TranslationComponent) entityB[ComponentType.Translation];
				RotationComponent rotB = (RotationComponent) entityB[ComponentType.Rotation];
				RigBodyComponent rigB = (RigBodyComponent) entityB[ComponentType.RigBody];
				ColliderComponent colB = (ColliderComponent) entityB[ComponentType.Collider];

				int ia = (int) colA.ColliderType;
				int ib = (int) colB.ColliderType;

				Collisions[ia, ib].HandleCollision(colA, trA, rotA, colB, trB, rotB, out ContactInfo info);

				if (info.ContactCount <= 0)
					continue;

				for (int k = 0; k < info.ContactCount; ++k)
				{
					float2 ra = info.Contacts[k] - trA.Value;
					float2 rb = info.Contacts[k] - trB.Value;
					float2 rv = rigB.Velocity + MathHelper.Cross(rigB.AngularVelocity, rb) - 
					            rigA.Velocity - MathHelper.Cross(rigA.AngularVelocity, ra);

					float contactVel = math.dot(rv, info.Normal);
					float raCrossN = MathHelper.Cross(ra, info.Normal);
					float rbCrossN = MathHelper.Cross(rb, info.Normal);
					float invMassSum = rigA.InvMass + rigB.InvMass + 
					                   raCrossN * raCrossN * rigA.InvInertia +
					                   rbCrossN * rbCrossN * rigB.InvInertia;

					float f = -contactVel / invMassSum / info.ContactCount;
					float2 impulse = info.Normal * f * deltaTime;

					rigA.Velocity -= rigA.InvMass * impulse;
					rigA.AngularVelocity += rigA.InvInertia * MathHelper.Cross(ra, -impulse);

					rigB.Velocity += rigB.InvMass * impulse;
					rigB.AngularVelocity += rigB.InvInertia * MathHelper.Cross(rb, impulse);
				}

				float2 correction = info.Penetration / (rigA.InvMass + rigB.InvMass) * info.Normal * 0.4f;
				trA.Value -= correction * rigA.InvMass;
				trB.Value += correction * rigB.InvMass;
			}

		}
	}
}