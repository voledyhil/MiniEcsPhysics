using System.Collections.Generic;
using MiniEcs.Core;
using Models;
using Models.Systems;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;


public class TestImpulseEngine : MonoBehaviour
{
	[SerializeField] private GameObject _staticBox;
	[SerializeField] private GameObject _staticCircle;
	
	[SerializeField] private GameObject _dynamicYellowBox;
	[SerializeField] private GameObject _dynamicYellowCircle;
	
	[SerializeField] private GameObject _dynamicBlueBox;
	[SerializeField] private GameObject _dynamicBlueCircle;

	[SerializeField] private Character _hero;

	private EcsWorld _world;
	private EcsEngine _engine;


	private readonly CollisionMatrix _collisionMatrix = new CollisionMatrix(
		new List<object>
		{
			new List<object> {"None", "yellow", "blue", "Default"},
			new List<object> {"Default", true, true, true},
			new List<object> {"blue", false, true},
			new List<object> {"yellow", true},
		});
	
	private readonly Dictionary<uint, Character> _gameObjects = new Dictionary<uint, Character>();

	private void Start()
	{
		_world = new EcsWorld(ComponentType.TotalComponents);
		
		_engine = new EcsEngine();
		_engine.AddSystem(new BroadphaseInitSystem());
		_engine.AddSystem(new IntegrateVelocitySystem());
		_engine.AddSystem(new BroadphaseUpdateSystem());
		_engine.AddSystem(new BroadphaseCalculatePairSystem(_collisionMatrix));
		_engine.AddSystem(new ResolveCollisionsSystem());
		_engine.AddSystem(new RaytracingSystem(_collisionMatrix));
		
		for (int i = 0; i < 7000; i++)
		{
			float x = (Random.value > 0.5 ? 1 : -1) * 2500 * Random.value;
			float y = (Random.value > 0.5 ? 1 : -1) * 2500 * Random.value;

			Vector2 hitPoint = new Vector2(x, y);
			if (Random.value >= 0.5)
			{
				float hw = Random.Range(5f, 10f);
				float hh = Random.Range(5f, 10f);

				CreateBox(hitPoint, new Vector2(hw, hh), true);
			}
			else
			{
				float radius = Random.Range(5f, 10f);
				CreateCircle(hitPoint, radius, true);
			}
		}

		for (int i = 0; i < 3000; i++)
		{
			float x = (Random.value > 0.5 ? 1 : -1) * 500 * Random.value;
			float y = (Random.value > 0.5 ? 1 : -1) * 500 * Random.value;

			Vector2 hitPoint = new Vector2(x, y);
			if (Random.value >= 0.5)
			{
				float hw = Random.Range(2f, 4f);
				float hh = Random.Range(2f, 4f);

				CreateBox(hitPoint, new Vector2(hw, hh), false);
			}
			else
			{
				float radius = Random.Range(2f, 4f);
				CreateCircle(hitPoint, radius, false);
			}
		}


		const float r = 5f;

		_heroEntity = _world.CreateEntity(
			new CircleColliderComponent(r) {Layer = _collisionMatrix.GetLayer("Default")},
			new RigBodyComponent
			{
				Inertia = 0
			}, 
			new RotationComponent(), 
			new TranslationComponent(),
			new RayComponent
			{
				Length = 150, 
				Layer = _collisionMatrix.GetLayer("Default")
			});
		
		_hero.ScaleTransform.localScale = new Vector3(2 * r, 1, 2 * r);
		_hero.Transform.position = Vector3.zero;
		_hero.Transform.rotation = Quaternion.Euler(0, 0, 0);

		_gameObjects.Add(_heroEntity.Id, _hero);
	}


	private float _elapsed;
	private EcsEntity _heroEntity;

	private void Update()
	{
		float dt = Time.deltaTime;//0.1f;

		_elapsed += Time.deltaTime;

		while (_elapsed >= dt)
		{
			_engine.Update(dt, _world);
			_elapsed -= dt;
		}

		DrawDynamicEntities();

		Transform camera = Camera.main.transform;
		camera.position = Vector3.Lerp(camera.position, _hero.Transform.position + 10 * Vector3.up, 10 * Time.deltaTime);

		
		RotationComponent rotation = (RotationComponent)_heroEntity[ComponentType.Rotation];
		RigBodyComponent rigBody = (RigBodyComponent)_heroEntity[ComponentType.RigBody];
		
		if (Input.GetKey(KeyCode.A))
		{
			rotation.Value += 2 * Time.deltaTime;
		}

		if (Input.GetKey(KeyCode.D))
		{
			rotation.Value -= 2 * Time.deltaTime;
		}

		rigBody.Velocity = float2.zero;

		if (Input.GetKey(KeyCode.W))
		{
			float rad = rotation.Value;
			float2 dir = new float2(-math.sin(rad), math.cos(rad));

			rigBody.Velocity = 500 * Time.deltaTime * dir;
		}
	}

	private void DrawDynamicEntities()
	{
		IEcsGroup group = _world.Filter(new EcsFilter()
			.AllOf(ComponentType.Translation, ComponentType.Rotation, ComponentType.Ray).NoneOf(ComponentType.RigBodyStatic));

		foreach (EcsEntity entity in group)
		{
			TranslationComponent translation = (TranslationComponent)entity[ComponentType.Translation];
			RotationComponent rotation = (RotationComponent)entity[ComponentType.Rotation];
			RayComponent ray = (RayComponent) entity[ComponentType.Ray];

			Character character = _gameObjects[entity.Id];
			character.Transform.position = new Vector3(translation.Value.x, 0, translation.Value.y);
			character.Transform.rotation = Quaternion.Euler(0, -Mathf.Rad2Deg * rotation.Value, 0);


			float2 hitPoint = ray.Hit ? ray.HitPoint : ray.Target;
			
			float distance = math.distance(ray.Source, hitPoint);
			character.Ray.localScale = new Vector3(0.4f, 5, distance);
			character.Ray.localPosition = 0.5f * distance * Vector3.forward;
		}
	}

	private void CreateCircle(Vector2 position, float radius, bool isStatic)
	{
		float rad = Random.Range(-math.PI, math.PI);
		CircleColliderComponent col = new CircleColliderComponent(radius);
		RigBodyComponent rig = new RigBodyComponent();
		EcsEntity entity = _world.CreateEntity(
			col,
			rig,
			new RotationComponent {Value = rad},
			new TranslationComponent {Value = position}
		);

		rig.ApplyForce(new float2(Random.Range(-100, 100), Random.Range(-100, 100)));

		if (isStatic)
		{
			rig.Mass = rig.Inertia = 0;
			entity[ComponentType.RigBodyStatic] = new RigBodyStaticComponent();
		}
		else
		{
			entity[ComponentType.Ray] = new RayComponent
			{
				Length = 150,
				Layer = _collisionMatrix.GetLayer("Default")
			};
		}

		col.Layer = isStatic
			? _collisionMatrix.GetLayer("Default")
			: _collisionMatrix.GetLayer(entity.Id % 2 == 0 ? "yellow" : "blue");


		Vector3 pos = new Vector3(position.x, 0, position.y);
		Quaternion rotation = Quaternion.Euler(0, -Mathf.Rad2Deg * rad, 0);
		GameObject go = Instantiate(isStatic ? _staticCircle : entity.Id % 2 == 0 ? _dynamicYellowCircle : _dynamicBlueCircle, pos, rotation);
		go.transform.localScale = new Vector3(2 * radius, 1, 2 * radius);
		go.transform.position = pos;
		go.transform.rotation = rotation;

		if (isStatic) 
			return;
		
		go.transform.localScale = Vector3.one;
		Character character = go.GetComponent<Character>();
		character.ScaleTransform.localScale = new Vector3(2 * radius, 1, 2 * radius);

		_gameObjects.Add(entity.Id, character);
	}


	private void CreateBox(Vector2 position, Vector2 size, bool isStatic)
	{
		float rad = Random.Range(-math.PI, math.PI);
		RectColliderComponent col = new RectColliderComponent(size);
		RigBodyComponent rig = new RigBodyComponent();
		EcsEntity entity = _world.CreateEntity(
			col,
			rig,
			new RotationComponent {Value = rad},
			new TranslationComponent {Value = position}
		);

		rig.ApplyForce(new float2(Random.Range(-100, 100), Random.Range(-100, 100)));

		if (isStatic)
		{
			rig.Mass = rig.Inertia = 0;
			entity[ComponentType.RigBodyStatic] = new RigBodyStaticComponent();
		}
		else
		{
			entity[ComponentType.Ray] = new RayComponent
			{
				Length = 150,
				Layer = _collisionMatrix.GetLayer("Default")
			};
		}

		col.Layer = isStatic
			? _collisionMatrix.GetLayer("Default")
			: _collisionMatrix.GetLayer(entity.Id % 2 == 0 ? "yellow" : "blue");


		Vector3 pos = new Vector3(position.x, 0, position.y);
		Quaternion rotation = Quaternion.Euler(0, -Mathf.Rad2Deg * rad, 0);
		GameObject go = Instantiate(isStatic ? _staticBox : entity.Id % 2 == 0 ? _dynamicYellowBox : _dynamicBlueBox, pos, rotation);
		go.transform.localScale = new Vector3(2 * size.x, 1, 2 * size.y);
		go.transform.position = pos;
		go.transform.rotation = rotation;

		if (isStatic) 
			return;
		
		go.transform.localScale = Vector3.one;
		Character character = go.GetComponent<Character>();
		character.ScaleTransform.localScale = new Vector3(2 * size.x, 1, 2 * size.y);

		_gameObjects.Add(entity.Id, character);
	}
}