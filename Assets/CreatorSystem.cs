using System;
using System.Collections.Generic;
using System.Linq;
using MiniEcs.Core;
using MiniEcs.Core.Systems;
using Physics;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

[EcsUpdateBefore(typeof(InputSystem))]
public class CreatorSystem : IEcsSystem
{
	private readonly PhysicsScene _physicsScene;
	private readonly CollisionMatrix _collisionMatrix;
		
	private readonly EcsFilter _staticRectFilter;
	private readonly EcsFilter _staticCircleFilter;
	private readonly EcsFilter _dynamicBlueCircleFilter;
	private readonly EcsFilter _dynamicYellowCircleFilter;
	private readonly EcsFilter _heroFilter;

	public CreatorSystem(PhysicsScene physicsScene, CollisionMatrix collisionMatrix)
	{
		_physicsScene = physicsScene;
		_collisionMatrix = collisionMatrix;
			
		_heroFilter = new EcsFilter().AllOf<HeroComponent>();
			
		_staticRectFilter = new EcsFilter().AllOf<StaticRectComponent, CharacterComponent, BroadphaseRefComponent>();
		_staticCircleFilter = new EcsFilter().AllOf<StaticCircleComponent, CharacterComponent, BroadphaseRefComponent>();
		_dynamicBlueCircleFilter = new EcsFilter().AllOf<BlueCircleComponent, CharacterComponent, BroadphaseRefComponent>();
		_dynamicYellowCircleFilter = new EcsFilter().AllOf<YellowCircleComponent, CharacterComponent, BroadphaseRefComponent>();
	}

	public void Update(float deltaTime, EcsWorld world)
	{
		CreateOrDestroyEntities(world, _staticRectFilter, _physicsScene.StaticRectCount, CreateStaticRect);
		CreateOrDestroyEntities(world, _staticCircleFilter, _physicsScene.StaticCircleCount, CreateStaticCircle);
		CreateOrDestroyEntities(world, _dynamicBlueCircleFilter, _physicsScene.DynamicBlueCircleCount, CreateDynamicBlueCircle);
		CreateOrDestroyEntities(world, _dynamicYellowCircleFilter, _physicsScene.DynamicYellowCircleCount, CreateDynamicYellowCircle);

		if (world.Filter(_heroFilter).CalculateCount() > 0) 
			return;
			
		IEcsEntity heroEntity = CreateCircleEntity(world, Vector2.zero, 0, 5, 1, "Default", 150);
		heroEntity.AddComponent(new HeroComponent());
		Instantiate(_physicsScene.Hero, heroEntity);
	}
		
	private static void CreateOrDestroyEntities(EcsWorld world, EcsFilter filter, int count, Action<EcsWorld> createEntity)
	{
		IEcsGroup group = world.Filter(filter);
		if (group.CalculateCount() == count) 
			return;

		IEcsEntity[] entities = group.ToEntityArray();
		for (int i = entities.Length; i < count; i++)
		{
			createEntity(world);
		}
			
		for (int i = count; i < entities.Length; i++)
		{
			IEcsEntity entity = entities[i];
			BroadphaseRefComponent brRef = entity.GetComponent<BroadphaseRefComponent>();
			CharacterComponent character = entity.GetComponent<CharacterComponent>();

			Object.Destroy(character.Ref.gameObject);

			foreach (SAPChunk chunk in brRef.Chunks)
				BroadphaseHelper.RemoveFormChunk(chunk, entity.Id);

			entity.Destroy();
		}
	}
		
	private static void CalculateTransform(out Vector2 position, out float rotation)
	{
		float x = (Random.value > 0.5 ? 1 : -1) * 1000 * Random.value;
		float y = (Random.value > 0.5 ? 1 : -1) * 1000 * Random.value;
		position = new Vector2(x, y);
		rotation = Random.Range(-math.PI, math.PI);
	}

	private void CreateDynamicYellowCircle(EcsWorld world)
	{
		CalculateTransform(out Vector2 position, out float rotation);
			
		float radius = Random.Range(2f, 4f);
		IEcsEntity circleEntity = CreateCircleEntity(world, position, rotation, radius, 1, "yellow", 0);
		circleEntity.AddComponent(new YellowCircleComponent());
		Instantiate(_physicsScene.DynamicYellowCircle, circleEntity);
	}

	private void CreateDynamicBlueCircle(EcsWorld world)
	{
		CalculateTransform(out Vector2 position, out float rotation);

		float radius = Random.Range(2f, 4f);
		IEcsEntity circleEntity = CreateCircleEntity(world, position, rotation, radius, 1, "blue", 100);
		circleEntity.AddComponent(new BlueCircleComponent());
		Instantiate(_physicsScene.DynamicBlueCircle, circleEntity);
	}

	private void CreateStaticRect(EcsWorld world)
	{
		CalculateTransform(out Vector2 position, out float rotation);

		Vector2 size = new Vector2(Random.Range(5f, 10f), Random.Range(5f, 10f));
		IEcsEntity rectEntity = CreateRectEntity(world, position, rotation, size, 0, "Default", 0);
		rectEntity.AddComponent(new StaticRectComponent());
		Instantiate(_physicsScene.StaticRect, rectEntity);
	}

	private void CreateStaticCircle(EcsWorld world)
	{
		CalculateTransform(out Vector2 position, out float rotation);

		float radius = Random.Range(5f, 10f);
		IEcsEntity circleEntity = CreateCircleEntity(world, position, rotation, radius, 0, "Default", 0);
		circleEntity.AddComponent(new StaticCircleComponent());
		Instantiate(_physicsScene.StaticCircle, circleEntity);
	}

	private static void Instantiate(GameObject prefab, IEcsEntity entity)
	{
		TransformComponent tr = entity.GetComponent<TransformComponent>();
		ColliderComponent col = entity.GetComponent<ColliderComponent>();

		Vector3 pos = new Vector3(tr.Position.x, 0, tr.Position.y);
		Quaternion rotation = Quaternion.Euler(0, -Mathf.Rad2Deg * tr.Rotation, 0);
		GameObject go = Object.Instantiate(prefab, pos, Quaternion.identity);
		go.transform.position = pos;
		go.transform.rotation = rotation;

		Character character = go.GetComponent<Character>();
		character.ScaleTransform.localScale = new Vector3(2 * col.Size.x, 1, 2 * col.Size.y);
		if (character.RayGameObject)
			character.RayGameObject.SetActive(entity.HasComponent<RayComponent>());
		entity.AddComponent(new CharacterComponent {Ref = character});
	}

	private IEcsEntity CreateCircleEntity(EcsWorld world, Vector2 position, float rotation, float radius, float mass, string colliderLayer, float rayLength)
	{
		return CreateEntity(world, position, rotation, new ColliderComponent
			{
				ColliderType = ColliderType.Circle,
				Size = radius,
				Layer = _collisionMatrix.GetLayer(colliderLayer)
			}, 
			mass, rayLength);
	}

	private IEcsEntity CreateRectEntity(EcsWorld world, Vector2 position, float rotation, Vector2 size, float mass,
		string colliderLayer, float rayLength)
	{
		return CreateEntity(world, position, rotation, new ColliderComponent
			{
				ColliderType = ColliderType.Rect,
				Size = size,
				Layer = _collisionMatrix.GetLayer(colliderLayer)
			},
			mass, rayLength);
	}

	private IEcsEntity CreateEntity(EcsWorld world, Vector2 position, float rotation, ColliderComponent col, float mass, float rayLength)
	{
		IEcsEntity entity = world.CreateEntity(
			col,
			new RigBodyComponent
			{
				Velocity = new float2(Random.Range(-10, 10), Random.Range(-10, 10)),
				Mass = mass
			},
			new TransformComponent {Position = position, Rotation = rotation}
		);

		if (mass <= 0)
		{
			entity.AddComponent(new RigBodyStaticComponent());
		}

		if (rayLength > 0)
		{
			entity.AddComponent(new RayComponent
			{
				Length = rayLength,
				Layer = _collisionMatrix.GetLayer("Default")
			});
		}

		return entity;
	}
}