using MiniEcs.Core;
using MiniEcs.Core.Systems;
using Models.Systems.Physics;
using Unity.Mathematics;
using UnityEngine;

namespace Models.Systems
{
    [EcsUpdateBefore(typeof(PhysicsSystemGroup))]
    public class InputSystem : IEcsSystem
    {
        private readonly EcsFilter _heroFilter;

        public InputSystem()
        {
            _heroFilter = new EcsFilter().AllOf(ComponentType.Transform, ComponentType.RigBody, ComponentType.Hero);
        }
        
        public void Update(float deltaTime, EcsWorld world)
        {
            foreach (EcsEntity entity in world.Filter(_heroFilter))
            {
                TransformComponent rotation = (TransformComponent)entity[ComponentType.Transform];
                RigBodyComponent rigBody = (RigBodyComponent)entity[ComponentType.RigBody];
		
                if (Input.GetKey(KeyCode.A))
                {
                    rotation.Rotation += 2 * deltaTime;
                }

                if (Input.GetKey(KeyCode.D))
                {
                    rotation.Rotation -= 2 * deltaTime;
                }

                rigBody.Velocity = float2.zero;

                if (!Input.GetKey(KeyCode.W)) 
                    continue;
                
                float rad = rotation.Rotation;
                float2 dir = new float2(-math.sin(rad), math.cos(rad));
                rigBody.Velocity = 25 * dir;
            }
            
        }
    }
}