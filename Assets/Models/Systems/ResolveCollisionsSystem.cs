using MiniEcs.Core;
using Unity.Mathematics;

namespace Models.Systems
{
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
				
				TranslationComponent translationA = (TranslationComponent)entityA[ComponentType.Translation];
				RotationComponent rotationA = (RotationComponent)entityA[ComponentType.Rotation];
				RigBodyComponent rigBodyA = (RigBodyComponent)entityA[ComponentType.RigBody];
				ColliderComponent colliderA = (ColliderComponent) entityA[ComponentType.Collider];

				TranslationComponent translationB = (TranslationComponent)entityB[ComponentType.Translation];
				RotationComponent rotationB = (RotationComponent)entityB[ComponentType.Rotation];
				RigBodyComponent rigBodyB = (RigBodyComponent)entityB[ComponentType.RigBody];
				ColliderComponent colliderB = (ColliderComponent) entityB[ComponentType.Collider];


				int ia = (int) colliderA.ColliderType;
				int ib = (int) colliderB.ColliderType;

				Collisions[ia, ib].HandleCollision(
					colliderA, translationA, rotationA, colliderB, translationB, rotationB, out ContactInfo info);

				if (info.ContactCount <= 0)
					continue;

				for (int k = 0; k < info.ContactCount; ++k)
				{
					float2 ra = info.Contacts[k] - translationA.Value;
					float2 rb = info.Contacts[k] - translationB.Value;
					float2 rv = rigBodyB.Velocity + MathHelper.Cross(rigBodyB.AngularVelocity, rb) - rigBodyA.Velocity -
					            MathHelper.Cross(rigBodyA.AngularVelocity, ra);

					float contactVel = math.dot(rv, info.Normal);
					float raCrossN = MathHelper.Cross(ra, info.Normal);
					float rbCrossN = MathHelper.Cross(rb, info.Normal);
					float invMassSum = rigBodyA.InvMass + rigBodyB.InvMass +
					                   raCrossN * raCrossN * rigBodyA.InvInertia +
					                   rbCrossN * rbCrossN * rigBodyB.InvInertia;

					float f = -contactVel / invMassSum / info.ContactCount;
					float2 impulse = info.Normal * f * deltaTime;

					rigBodyA.Velocity -= rigBodyA.InvMass * impulse;
					rigBodyA.AngularVelocity += rigBodyA.InvInertia * MathHelper.Cross(ra, -impulse);

					rigBodyB.Velocity += rigBodyB.InvMass * impulse;
					rigBodyB.AngularVelocity += rigBodyB.InvInertia * MathHelper.Cross(rb, impulse);
				}

				float2 correction = info.Penetration / (rigBodyA.InvMass + rigBodyB.InvMass) * info.Normal * 0.4f;
				translationA.Value -= correction * rigBodyA.InvMass;
				translationB.Value += correction * rigBodyB.InvMass;
			}

		}
	}
}