using MiniEcs.Core;
using MiniEcs.Core.Systems;

namespace Models.Systems.Physics
{
    [EcsUpdateInGroup(typeof(PhysicsSystemGroup))]
    [EcsUpdateAfter(typeof(BroadphaseInitSystem))]
    [EcsUpdateBefore(typeof(BroadphaseUpdateSystem))]
    public class IntegrateVelocitySystem : IEcsSystem
    {
        private readonly EcsFilter _filter;

        public IntegrateVelocitySystem()
        {
            _filter = new EcsFilter()
                .AllOf(ComponentType.Transform, ComponentType.RigBody).NoneOf(ComponentType.RigBodyStatic);
        }

        public void Update(float deltaTime, EcsWorld world)
        {
            foreach (EcsEntity entity in world.Filter(_filter))
            {
                TransformComponent transform = (TransformComponent) entity[ComponentType.Transform];
                RigBodyComponent rigBody = (RigBodyComponent) entity[ComponentType.RigBody];

                transform.Position += rigBody.Velocity * deltaTime;
                transform.Rotation += rigBody.AngularVelocity * deltaTime;
            }
        }
    }
}