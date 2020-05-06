using System;
using System.Collections.Generic;
using System.Linq;
using MiniEcs.Core;
using MiniEcs.Core.Systems;
using Models.Systems.Physics;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Models.Systems
{
	[EcsUpdateBefore(typeof(InputSystem))]
	public class CreatorSystem : IEcsSystem
	{
		private readonly PhysicsScene _physicsScene;
		private readonly CollisionMatrix _collisionMatrix;
		
		private readonly EcsFilter _staticRectFilter;
		private readonly EcsFilter _staticCircleFilter;
		private readonly EcsFilter _dynamicBlueRectFilter;
		private readonly EcsFilter _dynamicBlueCircleFilter;
		private readonly EcsFilter _dynamicYellowRectFilter;
		private readonly EcsFilter _dynamicYellowCircleFilter;
		private readonly EcsFilter _heroFilter;

		public CreatorSystem(PhysicsScene physicsScene, CollisionMatrix collisionMatrix)
		{
			_physicsScene = physicsScene;
			_collisionMatrix = collisionMatrix;
			
			_heroFilter = new EcsFilter().AllOf(ComponentType.Hero);
			
			_staticRectFilter = new EcsFilter().AllOf(ComponentType.StaticRect, ComponentType.Character, ComponentType.BroadphaseRef);
			_staticCircleFilter = new EcsFilter().AllOf(ComponentType.StaticCircle, ComponentType.Character, ComponentType.BroadphaseRef);
			_dynamicBlueRectFilter = new EcsFilter().AllOf(ComponentType.BlueRect, ComponentType.Character, ComponentType.BroadphaseRef);
			_dynamicBlueCircleFilter = new EcsFilter().AllOf(ComponentType.BlueCircle, ComponentType.Character, ComponentType.BroadphaseRef);
			_dynamicYellowRectFilter = new EcsFilter().AllOf(ComponentType.YellowRect, ComponentType.Character, ComponentType.BroadphaseRef);
			_dynamicYellowCircleFilter = new EcsFilter().AllOf(ComponentType.YellowCircle, ComponentType.Character, ComponentType.BroadphaseRef);
		}

		public void Update(float deltaTime, EcsWorld world)
		{
			CreateOrDestroyEntities(world, _staticRectFilter, _physicsScene.StaticRectCount, CreateStaticRect);
			CreateOrDestroyEntities(world, _staticCircleFilter, _physicsScene.StaticCircleCount, CreateStaticCircle);
			CreateOrDestroyEntities(world, _dynamicBlueRectFilter, _physicsScene.DynamicBlueRectCount, CreateDynamicBlueRect);
			CreateOrDestroyEntities(world, _dynamicBlueCircleFilter, _physicsScene.DynamicBlueCircleCount, CreateDynamicBlueCircle);
			CreateOrDestroyEntities(world, _dynamicYellowRectFilter, _physicsScene.DynamicYellowRectCount, CreateDynamicYellowRect);
			CreateOrDestroyEntities(world, _dynamicYellowCircleFilter, _physicsScene.DynamicYellowCircleCount, CreateDynamicYellowCircle);

			if (world.Filter(_heroFilter).CalculateCount() > 0) 
				return;
			
			EcsEntity heroEntity = CreateCircleEntity(world, Vector2.zero, 0, 5, 1, 1, "Default", 150);
			heroEntity[ComponentType.Hero] = new HeroComponent();
			Instantiate(_physicsScene.Hero, heroEntity);
		}
		
		private static void CreateOrDestroyEntities(EcsWorld world, EcsFilter filter, int count, Action<EcsWorld> createEntity)
		{
			IEcsGroup group = world.Filter(filter);
			if (group.CalculateCount() == count) 
				return;
			
			List<EcsEntity> entities = group.ToList();
			for (int i = entities.Count; i < count; i++)
			{
				createEntity(world);
			}
			
			for (int i = count; i < entities.Count; i++)
			{
				EcsEntity entity = entities[i];
				BroadphaseRefComponent brRef = (BroadphaseRefComponent) entity[ComponentType.BroadphaseRef];
				CharacterComponent character = (CharacterComponent) entity[ComponentType.Character];

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
			EcsEntity circleEntity = CreateCircleEntity(world, position, rotation, radius, 1, 1, "yellow", 0);
			circleEntity[ComponentType.YellowCircle] = new YellowCircleComponent();
			Instantiate(_physicsScene.DynamicYellowCircle, circleEntity);
		}

		private void CreateDynamicYellowRect(EcsWorld world)
		{
			CalculateTransform(out Vector2 position, out float rotation);

			Vector2 size = new Vector2(Random.Range(2f, 4f), Random.Range(2f, 4f));
			EcsEntity rectEntity = CreateRectEntity(world, position, rotation, size, 1, 1, "yellow", 0);
			rectEntity[ComponentType.YellowRect] = new YellowRectComponent();
			Instantiate(_physicsScene.DynamicYellowRect, rectEntity);
		}

		private void CreateDynamicBlueCircle(EcsWorld world)
		{
			CalculateTransform(out Vector2 position, out float rotation);

			float radius = Random.Range(2f, 4f);
			EcsEntity circleEntity = CreateCircleEntity(world, position, rotation, radius, 1, 1, "blue", 100);
			circleEntity[ComponentType.BlueCircle] = new BlueCircleComponent();
			Instantiate(_physicsScene.DynamicBlueCircle, circleEntity);
		}

		private void CreateDynamicBlueRect(EcsWorld world)
		{
			CalculateTransform(out Vector2 position, out float rotation);

			Vector2 size = new Vector2(Random.Range(2f, 4f), Random.Range(2f, 4f));
			EcsEntity rectEntity = CreateRectEntity(world, position, rotation, size, 1, 1, "blue", 100);
			rectEntity[ComponentType.BlueRect] = new BlueRectComponent();
			Instantiate(_physicsScene.DynamicBlueBox, rectEntity);
		}

		private void CreateStaticRect(EcsWorld world)
		{
			CalculateTransform(out Vector2 position, out float rotation);

			Vector2 size = new Vector2(Random.Range(5f, 10f), Random.Range(5f, 10f));
			EcsEntity rectEntity = CreateRectEntity(world, position, rotation, size, 0, 0, "Default", 0);
			rectEntity[ComponentType.StaticRect] = new StaticRectComponent();
			Instantiate(_physicsScene.StaticRect, rectEntity);
		}

		private void CreateStaticCircle(EcsWorld world)
		{
			CalculateTransform(out Vector2 position, out float rotation);

			float radius = Random.Range(5f, 10f);
			EcsEntity circleEntity = CreateCircleEntity(world, position, rotation, radius, 0, 0, "Default", 0);
			circleEntity[ComponentType.StaticCircle] = new StaticCircleComponent();
			Instantiate(_physicsScene.StaticCircle, circleEntity);
		}

		private static void Instantiate(GameObject prefab, EcsEntity entity)
		{
			TransformComponent tr = (TransformComponent) entity[ComponentType.Transform];
			ColliderComponent col = (ColliderComponent) entity[ComponentType.Collider];

			Vector3 pos = new Vector3(tr.Position.x, 0, tr.Position.y);
			Quaternion rotation = Quaternion.Euler(0, -Mathf.Rad2Deg * tr.Rotation, 0);
			GameObject go = Object.Instantiate(prefab, pos, Quaternion.identity);
			go.transform.position = pos;
			go.transform.rotation = rotation;

			Character character = go.GetComponent<Character>();
			character.ScaleTransform.localScale = new Vector3(2 * col.Size.x, 1, 2 * col.Size.y);
			if (character.RayGameObject)
				character.RayGameObject.SetActive(entity[ComponentType.Ray] != null);
			entity[ComponentType.Character] = new CharacterComponent {Ref = character};
		}

		private EcsEntity CreateCircleEntity(EcsWorld world, Vector2 position, float rotation, float radius, float mass, float inertia,
			string colliderLayer, float rayLength)
		{
			return CreateEntity(world, position, rotation, new CircleColliderComponent
			{
				Radius = radius,
				Layer = _collisionMatrix.GetLayer(colliderLayer)
			}, 
			mass, inertia, rayLength);
		}

		private EcsEntity CreateRectEntity(EcsWorld world, Vector2 position, float rotation, Vector2 size, float mass, float inertia,
			string colliderLayer, float rayLength)
		{
			return CreateEntity(world, position, rotation, new RectColliderComponent
			{
				RectSize = size,
				Layer = _collisionMatrix.GetLayer(colliderLayer)
			}, 
			mass, inertia, rayLength);
		}

		private EcsEntity CreateEntity(EcsWorld world, Vector2 position, float rotation, IEcsComponent col, float mass,
			float inertia, float rayLength)
		{
			EcsEntity entity = world.CreateEntity(
				col,
				new RigBodyComponent
				{
					Velocity = new float2(Random.Range(-10, 10), Random.Range(-10, 10)),
					Mass = mass,
					Inertia = inertia,
				},
				new TransformComponent {Position = position, Rotation = rotation}
			);

			if (mass <= 0)
			{
				entity[ComponentType.RigBodyStatic] = new RigBodyStaticComponent();
			}

			if (rayLength > 0)
			{
				entity[ComponentType.Ray] = new RayComponent
				{
					Length = rayLength,
					Layer = _collisionMatrix.GetLayer("Default")
				};
			}

			return entity;
		}
	}
}