# MiniEcsPhysics
Implementing a simple 2D isometric physical using [MiniEcs framework](https://github.com/voledyhil/MiniEcs)

### Requirement
Unity 2019.3.x or later
  
### General
- Broad phase collision detection. Implementation algorithm Sweep and prune
- Narrow phase collision detection and resolve collision detection
- Layer-based collision detection
- Implementation a fast voxel traversal algorithm for ray tracing and collision detection ray vs colliders.
- Simple implementation of a Rigid Bodies

![Preview](/images/preview.gif)

## References
1. How to Create a Custom 2D Physics Engine: The Basics and Impulse Resolution (https://gamedevelopment.tutsplus.com/tutorials/how-to-create-a-custom-2d-physics-engine-the-basics-and-impulse-resolution--gamedev-6331)

2. A Fast Voxel Traversal Algorithm for Ray Tracing (http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.42.3443&rep=rep1&type=pdf)

3. Efficient Large-Scale Sweep and Prune Methods with AABB Insertion and Removal (https://www.math.ucsd.edu/~sbuss/ResearchWeb/EnhancedSweepPrune/SAP_paper_online.pdf)

4. Sweep And Prune (http://www.gamedev.ru/code/terms/SAP)
