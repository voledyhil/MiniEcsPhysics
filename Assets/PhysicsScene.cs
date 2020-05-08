using System.Collections.Generic;
using MiniEcs.Components;
using MiniEcs.Core;
using MiniEcs.Core.Systems;
using Physics;
using UnityEngine;

public class PhysicsScene : MonoBehaviour
{	
	[SerializeField] public GameObject StaticRect;
	[SerializeField] public GameObject StaticCircle;
	[SerializeField] public GameObject DynamicYellowCircle;
	[SerializeField] public GameObject DynamicBlueCircle;

	[SerializeField] public GameObject Hero;
	
	[Space]
	[Range(0, 10000)] 
	[SerializeField] public int StaticRectCount = 1000;
	[Range(0, 10000)]
	[SerializeField] public int StaticCircleCount = 1000;
	[Range(0, 10000)]
	[SerializeField] public int DynamicBlueCircleCount = 1000;
	[Range(0, 10000)]
	[SerializeField] public int DynamicYellowCircleCount = 1000;

	private EcsWorld _world;
	private EcsSystemGroup _engine;

	private readonly CollisionMatrix _collisionMatrix = new CollisionMatrix(
		new List<object>
		{
			new List<object> {"None", "yellow", "blue", "Default"},
			new List<object> {"Default", true, true, true},
			new List<object> {"blue", false, true},
			new List<object> {"yellow", true},
		});

	
	private void Start()
	{
		_world = new EcsWorld(ComponentType.TotalComponents);

		_engine = new EcsSystemGroup();
		
		_engine.AddSystem(new BroadphaseInitSystem());
		_engine.AddSystem(new IntegrateVelocitySystem());
		_engine.AddSystem(new BroadphaseUpdateSystem());
		_engine.AddSystem(new BroadphaseCalculatePairSystem(_collisionMatrix));
		_engine.AddSystem(new ResolveCollisionsSystem());
		_engine.AddSystem(new RaytracingSystem(_collisionMatrix));
		_engine.AddSystem(new BroadphaseClearPairSystem());
		
		_engine.AddSystem(new CreatorSystem(this, _collisionMatrix));
		_engine.AddSystem(new InputSystem());
		_engine.AddSystem(new PresenterSystem());
	}

	private void Update()
	{
		_engine.Update(Time.deltaTime, _world);
	}
}
