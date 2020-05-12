using System;
using System.Collections.Generic;
using MiniEcs.Core;
using MiniEcs.Core.Systems;
using Unity.Mathematics;

namespace Physics
{
    [EcsUpdateInGroup(typeof(PhysicsSystemGroup))]
    [EcsUpdateAfter(typeof(BroadphaseUpdateSystem))]
    [EcsUpdateBefore(typeof(ResolveCollisionsSystem))]
    public class BroadphaseCalculatePairSystem : IEcsSystem
    {
        private readonly CollisionMatrix _collisionMatrix;
        private readonly AABBComparer _comparer = new AABBComparer();

        public BroadphaseCalculatePairSystem(CollisionMatrix collisionMatrix)
        {
            _collisionMatrix = collisionMatrix;
        }

        public void Update(float deltaTime, EcsWorld world)
        {
            BroadphaseSAPComponent bpChunks = world.GetOrCreateSingleton<BroadphaseSAPComponent>();

            bpChunks.Pairs.Clear();
            foreach (SAPChunk chunk in bpChunks.Chunks.Values)
            {
                if (chunk.NeedRebuild)
                    BroadphaseHelper.BuildChunks(chunk);

                if (chunk.DynamicCounter > 0 || chunk.IsDirty)
                {
                    CalculatePairs(chunk);
                    chunk.IsDirty = chunk.DynamicCounter != 0;
                }

                for (int i = 0; i < chunk.PairLength; i++)
                    bpChunks.Pairs.Add(chunk.Pairs[i]);
            }
        }

        private unsafe void CalculatePairs(SAPChunk chunk)
        {
            chunk.PairLength = 0;

            int length = chunk.Length;

            _comparer.UpdateSortAxis(chunk.SortAxis);
            Array.Sort(chunk.Items, 0, length, _comparer);

            float2 s = float2.zero;
            float2 s2 = float2.zero;

            for (int i = 0; i < length; i++)
            {
                BroadphaseAABB a = chunk.Items[i];
                float2 p = (a.AABB->Min + a.AABB->Max) * 0.5f;

                s += p;
                s2 += p * p;

                for (int j = i + 1; j < length; j++)
                {
                    BroadphaseAABB b = chunk.Items[j];
                    if (b.AABB->Min[chunk.SortAxis] > a.AABB->Max[chunk.SortAxis])
                        break;

                    if (a.IsStatic && b.IsStatic)
                        continue;

                    if (!a.AABB->Overlap(*b.AABB))
                        continue;

                    if (!_collisionMatrix.Check(a.Layer, b.Layer))
                        continue;

                    if (chunk.PairLength >= chunk.Pairs.Length)
                        Array.Resize(ref chunk.Pairs, 2 * chunk.PairLength);

                    chunk.Pairs[chunk.PairLength++] = new BroadphasePair(a.Entity, b.Entity);
                }
            }

            float2 v = s2 / length - s * s / (length * length);

            chunk.SortAxis = v[1] > v[0] ? 1 : 0;
        }

        private unsafe class AABBComparer : IComparer<BroadphaseAABB>
        {
            private int _axis;

            public int Compare(BroadphaseAABB a, BroadphaseAABB b)
            {
                float minA = a.AABB->Min[_axis];
                float minB = b.AABB->Min[_axis];
                return minA < minB ? -1 : minA > minB ? 1 : 0;
            }

            public void UpdateSortAxis(int axis)
            {
                _axis = axis;
            }
        }
    }
}
