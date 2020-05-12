using MiniEcs.Core;
using MiniEcs.Core.Systems;

namespace Physics
{
    [EcsUpdateInGroup(typeof(PhysicsSystemGroup))]
    [EcsUpdateAfter(typeof(RaytracingSystem))]
    public class BroadphaseClearPairSystem : IEcsSystem
    {
        public void Update(float deltaTime, EcsWorld world)
        {
            BroadphaseSAPComponent bpChunks = world.GetOrCreateSingleton<BroadphaseSAPComponent>();
            
            bpChunks.Pairs.Clear();
        }
    }
}