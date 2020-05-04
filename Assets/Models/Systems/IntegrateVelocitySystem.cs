using MiniEcs.Core;

namespace Models.Systems
{
    [EcsUpdateAfter(typeof(BroadphaseInitSystem))]
    [EcsUpdateBefore(typeof(BroadphaseUpdateSystem))]
    public class IntegrateVelocitySystem : IEcsSystem
    {
        private readonly EcsFilter _filter;

        public IntegrateVelocitySystem()
        {
            _filter = new EcsFilter()
                .AllOf(ComponentType.Translation, ComponentType.Rotation, ComponentType.RigBody)
                .NoneOf(ComponentType.RigBodyStatic);
        }

        public void Update(float deltaTime, EcsWorld world)
        {
            foreach (EcsEntity entity in world.Filter(_filter))
            {
                TranslationComponent translation = (TranslationComponent) entity[ComponentType.Translation];
                RotationComponent rotation = (RotationComponent) entity[ComponentType.Rotation];
                RigBodyComponent rigBody = (RigBodyComponent) entity[ComponentType.RigBody];

                translation.Value += rigBody.Velocity * deltaTime;
                rotation.Value += rigBody.AngularVelocity * deltaTime;
            }
        }
    }
}