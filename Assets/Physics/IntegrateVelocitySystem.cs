using MiniEcs.Core;
using MiniEcs.Core.Systems;

namespace Physics
{
    [EcsUpdateInGroup(typeof(PhysicsSystemGroup))]
    [EcsUpdateAfter(typeof(BroadphaseInitSystem))]
    [EcsUpdateBefore(typeof(BroadphaseUpdateSystem))]
    public class IntegrateVelocitySystem : IEcsSystem
    {
        private readonly EcsFilter _filter;

        public IntegrateVelocitySystem()
        {
            _filter = new EcsFilter().AllOf<TransformComponent, RigBodyComponent>().NoneOf<RigBodyStaticComponent>();
        }

        public void Update(float deltaTime, EcsWorld world)
        {
            foreach (EcsEntity entity in world.Filter(_filter))
            {
                TransformComponent transform = entity.GetComponent<TransformComponent>();
                RigBodyComponent rigBody = entity.GetComponent<RigBodyComponent>();

                transform.Position += rigBody.Velocity * deltaTime;
            }
        }
    }
}