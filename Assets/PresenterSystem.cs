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
        _transformsFilter = new EcsFilter().AllOf<TransformComponent, CharacterComponent>().NoneOf<RigBodyStaticComponent>();
        _heroFilter = new EcsFilter().AllOf<HeroComponent, CharacterComponent>();
        _rayFilter = new EcsFilter().AllOf<CharacterComponent, RayComponent>();
    }

    public void Update(float deltaTime, EcsWorld world)
    {
        world.Filter(_transformsFilter).ForEach((IEcsEntity entity, TransformComponent transform, CharacterComponent character) =>
        {
            character.Ref.Transform.position = new Vector3(transform.Position.x, 0, transform.Position.y);
            character.Ref.Transform.rotation = Quaternion.Euler(0, -Mathf.Rad2Deg * transform.Rotation, 0);
        });

        world.Filter(_rayFilter).ForEach((IEcsEntity entity, RayComponent ray, CharacterComponent character) =>
        {
            float2 target = ray.Hit ? ray.HitPoint : ray.Target;

            float distance = math.distance(ray.Source, target);
            character.Ref.Ray.localScale = new Vector3(0.4f, 5, distance);
            character.Ref.Ray.localPosition = 0.5f * distance * Vector3.forward;
        });

        world.Filter(_heroFilter).ForEach((IEcsEntity entity, CharacterComponent character) =>
        {
            Transform camera = Camera.main.transform;
            camera.position = Vector3.Lerp(camera.position, character.Ref.transform.position + 10 * Vector3.up,
                10 * Time.deltaTime);
        });

    }
}