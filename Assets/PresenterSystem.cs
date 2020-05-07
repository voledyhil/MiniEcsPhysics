using MiniEcs.Components;
using MiniEcs.Core;
using MiniEcs.Core.Systems;
using Physics;
using Unity.Mathematics;
using UnityEngine;

[EcsUpdateAfter(typeof(PhysicsSystemGroup))]
public class PresenterSystem : IEcsSystem
{
    private readonly EcsFilter _transformsFilter;
    private readonly EcsFilter _rayFilter;
    private readonly EcsFilter _heroFilter;

    public PresenterSystem()
    {
        _transformsFilter = new EcsFilter()
            .AllOf(ComponentType.Transform, ComponentType.Character).NoneOf(ComponentType.RigBodyStatic);
        _heroFilter = new EcsFilter().AllOf(ComponentType.Hero, ComponentType.Character);
        _rayFilter = new EcsFilter().AllOf(ComponentType.Character, ComponentType.Ray);
    }
        
    public void Update(float deltaTime, EcsWorld world)
    {
        foreach (EcsEntity entity in world.Filter(_transformsFilter))
        {
            TransformComponent transform = (TransformComponent) entity[ComponentType.Transform];
            CharacterComponent character = (CharacterComponent) entity[ComponentType.Character];
			
            character.Ref.Transform.position = new Vector3(transform.Position.x, 0, transform.Position.y);
            character.Ref.Transform.rotation = Quaternion.Euler(0, -Mathf.Rad2Deg * transform.Rotation, 0);
        }

        foreach (EcsEntity entity in world.Filter(_rayFilter))
        {
            RayComponent ray = (RayComponent) entity[ComponentType.Ray];
            CharacterComponent character = (CharacterComponent) entity[ComponentType.Character];

            float2 target = ray.Hit ? ray.HitPoint : ray.Target;

            float distance = math.distance(ray.Source, target);
            character.Ref.Ray.localScale = new Vector3(0.4f, 5, distance);
            character.Ref.Ray.localPosition = 0.5f * distance * Vector3.forward;
        }

        foreach (EcsEntity entity in world.Filter(_heroFilter))
        {
            CharacterComponent character = (CharacterComponent) entity[ComponentType.Character];

            Transform camera = Camera.main.transform;
            camera.position = Vector3.Lerp(camera.position, character.Ref.transform.position + 10 * Vector3.up, 10 * Time.deltaTime);                
        }

    }
}