using MiniEcs.Core;
using MiniEcs.Core.Systems;
using Physics;
using Unity.Mathematics;
using UnityEngine;

[EcsUpdateBefore(typeof(PhysicsSystemGroup))]
public class InputSystem : IEcsSystem
{
    private readonly EcsFilter _heroFilter;

    public InputSystem()
    {
        _heroFilter = new EcsFilter().AllOf<TransformComponent, RigBodyComponent, HeroComponent>();
    }

    public void Update(float deltaTime, EcsWorld world)
    {
        world.Filter(_heroFilter).ForEach((IEcsEntity entity, TransformComponent transform, RigBodyComponent rigBody) =>
        {
            if (Input.GetKey(KeyCode.A))
            {
                transform.Rotation += 2 * deltaTime;
            }

            if (Input.GetKey(KeyCode.D))
            {
                transform.Rotation -= 2 * deltaTime;
            }

            rigBody.Velocity = float2.zero;

            if (!Input.GetKey(KeyCode.W))
                return;

            float rad = transform.Rotation;
            float2 dir = new float2(-math.sin(rad), math.cos(rad));
            rigBody.Velocity = 25 * dir;
        });
    }
}