# Unity Physics 101

In this document, we’ll first explain fundamental concepts of game physics, and then we’ll cover [essentials of the Unity Physics API](#unity-physics-api). Lastly, we’ll walk through a handful of very simple [samples which demonstrate these essentials](#physics-samples).

# What does a physics engine do?

Before getting into the specifics of the Unity Physics package, we’ll start with a general discussion of what physics engines typically do and how they work.

Every update, a physics engine does four main steps, in this order:

1. **Build a spatial data structure** of some kind that allows for efficient “intersection tests”. An intersection test checks whether the geometries of two physics objects touch or overlap.  
2. **Detect collisions** and generate *constraints* from the contact points between all touching or overlapping physics objects.  
3. **Run the physics “solver”** to update the physics objects' velocities based on their constraints and other factors, such as their masses, gravity, and friction.  
4. **Move the physics objects** based on their new velocities.

In your game code, the spatial data structure built by the physics engine may also be used for various purposes of game logic. For example, when a gun is fired, your game code can query the spatial structure to determine what the bullet might hit along its path.

**Note**: Because the delta time between frames often varies and because the physics simulation shouldn’t necessarily update at the same rate as the rendering or game simulation, a physics simulation is usually updated at its own fixed update rate. This is achieved by updating the physics simulation each frame 0 or more times to approximate an average rate over time. For example, one frame may perform 0 physics updates, but the next frame may perform 3, and the one after that just 1, *etc*, with the aim of averaging out over time to match the intended physics update rate.

## Rigid bodies

In most game physics engines, the physics objects are usually *rigid bodies*, meaning they are solid objects with uniformly distributed mass which do not bend, squish, or change shape in any way. This is in contrast to *soft bodies* (deformable bodies like rubber), fluids, gasses, and particles like sand.

**Note**: You might think rope, wire, cords, and cloth are deformable soft bodies, but they actually can be approximated as rigid body segments held together by constraints (see “joints” below). Often, however, these objects are handled by a separate, special solver for better efficiency and accuracy.

**Note**: Some rigid body simulations also support destructible objects, allowing the objects to fracture and break apart.

Typically, the data for each rigid body object includes these elements:

* position  
* orientation  
* collider (the geometry used in collision detection)  
* linear velocity (direction and speed of travel)  
* angular velocity (direction and speed of rotation)  
* mass  
* center of mass (a point, usually inside the object)  
* inertia tensor (the rotational semi-equivalent of mass)

...plus other optional elements:

* linear and angular damping (decay of velocities)  
* gravity (per-object override of the global gravity)  
* friction coefficient  
* restitution (bounciness)  
* collision filters

## Colliders

The following collider shapes are supported in Unity Physics:

* **Box**: A rectangular prism defined by a length, height, and width.  
* **Sphere**: A sphere defined by a radius.  
* **Capsule**: A cylinder with round ends, defined by an inner line segment and a radius.  
* **Cylinder**: A cylinder defined by a height and radius.  
* **Triangle / Quad**: 3 or 4 coplanar vertices forming either a triangle or a quad.  
* **Convex hulls**: An arbitrary convex hull defined by a set of 3D points.  
* **Mesh**: A mesh composed of triangles and quads.  
* **Compound**: A composite of multiple collider shapes.  
* **Terrain**: A uniform grid of height samples.

Collision detection for mesh colliders is much more computationally expensive than for primitive colliders, such as spheres and boxes. Convex hull colliders are also more expensive than primitives but are still considerably cheaper than mesh colliders. The cost of mesh colliders and convex hull colliders scales with their geometric complexity.

For performance and gameplay reasons, an object's collider (*a.k.a.* collision geometry) very often differs from its visible geometry. For example, using the full animated mesh of a character for collision detection can have a large performance cost, and such shapes can easily get caught on the environment as the character tries to move around. Therefore, animated characters are often instead given simple shape colliders, such as capsules, which make the collisions much cheaper and less likely to get caught on the environment.

**Note**: In some games, you might want characters to have accurate collisions for specific purposes, such as hit detection with bullets. In these scenarios, you can use a simple collider for character movements but also use a second, invisible, more detailed collider which moves with the character but which only collides with bullets. Such setups are also common use cases for collision filters (discussed later).

## Compound colliders

A compound collider is a single logical collider formed from multiple colliders. The combined, resulting shape need not be convex, and the separate shapes need not touch or intersect.

Compound colliders are useful when:

* …you want a complex collider shape but want cheaper collisions than the equivalent mesh collider.  
* …you want to distinguish between collisions for different parts of the object. For example, for a character, you want to distinguish between bullet collisions with the head, torso, or other body parts.

## Dynamic and kinematic bodies

A *dynamic* body is a “normal” rigid body physics object, meaning a body affected by gravity and forces generated from its collisions.

**Note**: In some physics engines, dynamic bodies that do not move for a short period may be ‘put to sleep’, which can improve performance by skipping their processing in certain stages of the simulation. For example, falling objects that come to rest on a surface may sleep once their momentum drops to zero (or very close to zero). Sleeping bodies will reawaken when affected by forces, such as when struck by other objects. [Havok Physics for Unity](https://docs.unity3d.com/Packages/com.havok.physics@latest) puts resting bodies to sleep, but Unity Physics itself has no such feature: a body in Unity Physics is always fully active.

A *kinematic* body is a rigid body with conceptually infinite mass. As such, its transform and velocity are not automatically affected by collisions or other interactions with other rigid bodies. Instead, the user’s own code is responsible for setting a kinematic body's transform and velocity to control how it moves.

**Note**: According to Wikipedia, “Kinematics is a subfield of physics and mathematics, developed in classical mechanics, that describes the motion of points, bodies (objects), and systems of bodies (groups of objects) without considering the forces that cause them to move.” So in a physics engine, the term “kinematic” makes sense for an object that does not (automatically) have forces applied to it.

Character controllers are a key use case for kinematic bodies. Though dynamic body character controllers are used in some games, they can make it difficult for the programmer to exert control: when the programmer directly sets a dynamic body’s position or velocity, they're effectively overriding the force of any collisions applied by the physics engine, leading to scenarios where the object could phase through other colliders and other undesirable behaviors. On the other hand, a kinematic controller does not automatically factor in collisions with other colliders, so the programmer must manually factor these collisions into the controller logic when updating the body's velocity.

More generally, kinematic bodies can be useful when you want a moving object that affects other objects when it collides with them but which itself does not automatically react to those collisions or other physical properties (*e.g.* gravity, friction, or damping). For example, an elevator should move other objects resting on its floor, so you need it to be a physics object; however, you generally want to control an elevator's movements strictly in your own code rather than allow the elevator to get stuck on obstructions or be affected by gravity, so you should make the elevator a kinematic body.

## Static colliders

A static collider is an object that has a collider but no velocity, mass, or other properties of a dynamic body. Generally, the objects that make up a game environment, such as static props, floors, walls, or terrains, should be static colliders.

In collisions between a static collider and dynamic or kinematic bodies, a static collider is treated as if it has infinite mass. In other words, a static collider is treated as an immovable object.

The physics engine does not check for collisions between static colliders.

**NOTE**: In Unity Physics, it's possible to move static colliders at runtime, but doing so triggers an expensive rebuild of the static collider bounding volume hierarchy used for collision detection. Generally you should avoid moving static colliders. For a collider that moves occasionally, consider using a kinematic with 0 velocity.

## Collision queries

It's often useful in your code to ask the physics engine questions like, "What would this object hit if it moved to this position or moved in this direction?” or, “What would a projectile hit if it were shot from here in this direction?", and so forth. Such questions are called *collision queries*.

The Unity Physics API provides five kinds of collision queries:

* **Overlap query**: Find all the colliders whose bounding boxes overlap a given shape.  
* **Ray cast query**: Find the intersections, if any, between any of the colliders and a given ray (a directed line segment).  
* **Collider cast query** (*a.k.a.* "shape cast" or "sweep"): Find the intersections, if any, between the colliders and a given shape which is moved along a given line segment.  
* **Collider distance query**: Find the shortest line segment connecting a given shape and each collider within a given maximum radius.  
* **Point distance query**: Find the shortest line segment connecting a given point and each collider within a given maximum radius.

## Collision and trigger events

For detected collisions between physics objects, the physics engine may generate *collision events* with information about the collisions. Processing these events in your code can be useful for many gameplay purposes, such as allowing your kinematic character controller to respond to collisions.

Because raising events for *every* collision could be very costly, physics engines typically only raise collision events for bodies designated by the user to raise events. 

A *trigger* is a (usually invisible) collider whose collisions do not generate constraints, so other bodies can intersect the trigger freely.

For detected collisions between a physics object and a trigger, the physics engine generates *trigger events* with information about the collisions. These events then can then be read in your code to watch for occurrences such as the player character walking into a certain area.

## Collision filtering

In many gameplay scenarios, you don't want every physics object to necessarily generate contact constraints and events when they collide with every other. For example, maybe you want projectiles in your game to pass through cosmetic debris, or maybe some moving objects that are tethered together should pass through each other.

To solve these problems, collision filters let you specify which individual objects or groups of objects should generate contact constraints and events when they collide with each other.

## The broad phase and narrow phases of collision detection

Because it can be quite costly to determine which pairs of physics bodies collide with each other, collision detection is usually split into two phases:

1. In the first phase, called the "broad" phase, the physics engine finds the colliders whose Axis-Aligned Bounding Boxes (AABBs) intersect. An AABB is a box which is aligned with the world axes (in other words, a box which is not rotated) and which fully surrounds the object.  
2. In the second phase, called the "narrow" phase, the pairs of objects whose AABBs intersected in the broad phase are checked to see if their actual colliders intersect.

Because AABB intersection tests are individually cheap (especially compared to hull and mesh intersection tests), splitting collision detection into broad and narrow phases typically reduces the number of pairwise intersection tests required for collision detection.

The broad phase is also often further accelerated by grouping the AABB's of the colliders into some kind of spatial structure, such as an [octree](https://en.wikipedia.org/wiki/Octree) or [Bounding Volume Hierarchy (BVH)](https://en.wikipedia.org/wiki/Bounding_volume_hierarchy), thus allowing the broad phase to only check for intersections between AABBs in the same part of the world.

Unity Physics creates a BVH for the dynamic bodies and kinematics, plus a separate additional BVH for static colliders.

**NOTE**: Be careful about large colliders: colliders with very large AABB's will often end up intersecting many other objects in the broad phase, possibly leading to an excessive number of expensive pairwise intersection tests in the narrow phase. In such cases, consider breaking the large collider into smaller pieces.

## Joints and motors

In addition to the contact constraints generated by the physics engine’s collision detection, you may sometimes wish to manually create additional constraints that last indefinitely. For example, you may wish to tether two bodies together such that they drag each other when moved. These persistent constraints between two bodies are called *joints*.

Depending upon a joint’s configuration, its constraints may restrict certain degrees of freedom (DoF), *e.g.* a joint might only allow relative rotation of the two bodies along a single axis.

[Unity Physics offers several kinds of joints](https://docs.unity3d.com/Packages/com.unity.physics@1.3/manual/custom-joints.html):

* **Ball and Socket**: Allows motion around an indefinite number of axes. Humans have such joints in the hips and shoulders.  
* **Limited Hinge**: Allows limited articulation on one axis. Humans have such joints in the fingers and knees.  
* **Fixed**: Constrains two rigid bodies together, removing their ability to move independently of each other.  
* **Hinge**: Allows free rotation on one axis. Can be used for spinning wheels and carousels.  
* **Prismatic**: Constrains two bodies to a sliding motion on one axis. Can be used to make various sliding doors.  
* **Ragdoll**: Limits the motion on a few axes. Useful for creating characters.  
* **Stiff Spring**: Constrains two bodies to be a certain distance apart from each other.

A *motor* is a variant of joint which has a “driven” constraint, meaning a constraint which induces force on the two bodies even when they otherwise are at rest.

[Unity Physics offers a few kinds of motors](https://docs.unity3d.com/Packages/com.unity.physics@1.3/manual/custom-motors.html):

* **Rotation Motor**: A motorized hinge joint that rotates about an axis toward a target angle.  
* **Angular Velocity Motor**: A motorized hinge joint that rotates about an axis at a constant target velocity.  
* **Position Motor**: A motorized prismatic joint that drives toward a relative target position.   
* **Linear Velocity Motor**: A motorized prismatic joint that drives to a relative target position at a constant target velocity.

---

# Unity Physics API {#unity-physics-api}

Now let’s get into the specifics of the Unity Physics package.

## Static collider entities

A static collider entity represents collision geometry that is generally expected not to move, such as the ground and walls that make up a typical game environment. Moving a static collider at runtime triggers an expensive rebuild of the static collider hierarchy, so it’s generally recommended that you move static colliders only rarely or not at all.

Static collider entities are defined by two components:

* **PhysicsCollider**: The core info about the collider, which includes:  
  * The collider geometry  
  * Collision filter properties  
  * Mass distribution properties  
  * Friction properties  
  * Restitution (bounciness) properties


    **Note:** The collision geometry is stored in a BlobAssetReference\<Collider\>. Because PhysicsCollider components of different entities may share the same BlobAssetReference\<Collider\>, changes to an individual collider may affect the colliders of multiple entities. To avoid this, you may need to give entities their [own separate copies of a collider](https://docs.unity3d.com/Packages/com.unity.physics@1.3/manual/physics-collider-components.html#making-colliders-unique-during-authoring).


* **PhysicsWorldIndex**: A shared component that denotes which “physics world” this entity belongs to. Physics entities only collide and interact with other physics entities of the same physics world.

  **Note:** Be clear that the concept of “physics world” is separate from the general Entities concept of entity worlds. Whereas an entity world contains a set of entities and systems that is logically isolated from other entity worlds, a physics world contains a subset of the physics entities *within* a single entity world. Usually, only the default physics world (0) is needed, but additional physics worlds can be useful in certain niche cases.

A compound collider (a collider made out of multiple separate pieces of geometry) may also have this component:

* **PhysicsColliderKeyEntityPair**: A buffer that references the colliders that make up a compound collider. 

## Rigid body entities

A dynamic rigid body entity represents a physics body with velocity, mass, and other physical properties.

A kinematic rigid body is a dynamic rigid body with infinite mass and inertia which therefore does not automatically respond to collision forces. Kinematic rigid bodies are commonly used for character and vehicle controllers, as well as other things that might be programmatically moved, like say an elevator or moving platform.

Rigid body entities have the components of a collider plus also:

* **PhysicsVelocity**: The linear and angular velocity of the body.  
* **PhysicsMass**: The mass, center of mass, inertia, and angular expansion factor.  
    
  **Note:** If PhysicsMass is absent, the mass and inertia default to infinite, making the body kinematic.  
    
  **Note:** The linear velocity of a PhysicsMass is expressed in world space, but the angular velocity is expressed relative to the PhysicsMass transform rather than world space. The “angular expansion factor” determines how much to expand a rigid body's AABB to enclose its swept volume.

          
Additional physical properties can be specified by adding these components:  
        

* **PhysicsDamping**: Damping applied to the velocities in each simulation step.  
* **PhysicsGravityFactor**: A gravity factor that modifies the global gravity for this body.  
* **PhysicsCustomTags**: User-customizable data which is copied to any collision or trigger events involving this body. Useful for classifying bodies in your event-processing code.  
* **PhysicsTemporalCoherenceInfo**:   
* **PhysicsTemporalCoherenceTag**:  
* **PhysicsMassOverride**: Contains a flag that can toggle the entity between kinematic and non-kinematic. Not required for kinematic rigid body entities unless they need to toggle between kinematic and non-kinematic.

A rigid body can also have a few additional graphics-related components:

* **PhysicsRenderEntity**: Sometimes you may wish to express a physical object and its graphical rendering as two separate entities. For a physics entity that isn’t itself rendered, this component stores a reference to the entity that is meant to be its graphical representation.  
* **PhysicsGraphicalSmoothing**: Specifies that the motion of the graphical representation entity should be smoothed (but only when the rendering framerate is greater than the physics fixed step rate).  
* **PhysicsGraphicalInterpolationBuffer**: Stores the state of a rigid body from the previous physics tick in order to interpolate the motion of the body's graphical representation. When used in conjunction with PhysicsGraphicalSmoothing, this component indicates that smoothing should interpolate between the two most recent physics simulation ticks, which results in a more accurate representation of the physics simulation (but with an added tick of latency).

## Joints and motors

A *joint* represents a set of constraints that connect two rigid body entities. A *motor* is a kind of joint with a “driving” constraint that applies forces on the two rigid bodies, such as a rotational spin force.

Each joint or motor is represented as an entity with these two components:  
        

* **PhysicsJoint**: The properties of the joint:  
  * Two BodyFrames (transforms which define the joint relative to the two bodies)  
  * A JointType enum (denotes the type of joint: Fixed, Hinge, RotationalMotor, *etc.*)  
  * A set of Constraints  
* **PhysicsConstrainedBodyPair**  
  * A pair of Entity ids  
  * An int denoting whether the two bodies should generate collision events

Joint and motor entities that are part of a set may also have the following component:

* **PhysicsJointCompanion**: A buffer referencing the other joint entities that form a complex joint configuration

  **Note**: Joints and motors do not necessarily have to be their own separate entities (though they often are). For example, a joint can be created by adding the PhysicsJoint and PhysicsConstrainedBodyPair components to one of the two rigid body entities connected by the joint. All that really matters is that a PhysicsJoint and its related PhysicsConstrainedBodyPair (plus optional PhysicsJointCompanion) are placed on the same entity.

### Additional singleton components

Two singleton components provide access to some core Unity Physics functionality:

* **SimulationSingleton**: Provides access to the Simulation, which is primarily used when you need to process collision, trigger, and impulse events.  
* **PhysicsWorldSingleton**: Provides access to the PhysicsWorld, which includes methods for ray casts and other collision queries.

Unity Physics will create entities with these two components automatically.

Two additional singleton components specify some global physics settings and enable some visual debugging tools:

* **PhysicsStep:** Contains properties that determine how the physics world is stepped. If no instance of this singleton exists, default values are used.  
* **PhysicsDebugDisplayData:** Contains properties that determine whether to draw physics debug visualizations, such as wireframes of the colliders. If no instance of this singleton exists, default values are used.

Unity Physics does not create entities with these two components automatically.

**Note:** The term “singleton” here means that these components should each be added to no more than one entity in the world.

---

## Creating physics entities in the editor

**Note:** This section assumes you have basic familiarity with entity baking.

To create physics entities in a subscene, you have three options:

1. Use the “**Built-in” physics components** (the standard GameObject physics components). Because these Built-in components were designed for PhysX, they don’t neatly map to Unity Physics in all regards, but they are sufficient for most common cases.  
2. Use the [**physics authoring components published in the Unity Physics package samples**](https://github.com/Unity-Technologies/EntityComponentSystemSamples/tree/master/PhysicsSamples/Assets/Samples/Unity%20Physics/1.3.5/Custom%20Physics%20Authoring). These authoring components were designed for Unity Physics and so map more accurately to its functionality.  
3. Make **your own custom authoring components**. If you study the authoring components from the package samples, you may learn enough to create your own that better fit your needs. Be warned, though, that some facets of the underlying Unity Physics component data (such as those related to joints and motors) require deep understanding of the physics engine to set up properly.

(For simplicity, we will focus on the first option in the samples below.)

When a GameObject with a collider component in a subscene is baked, the created entity is given the Unity.Physics components PhysicsCollider and PhysicsWorldIndex.

When a GameObject with a RigidBody component in a subscene is baked, the created entity is given the Unity.Physics components PhysicsVelocity, PhysicsMass, and PhysicsDamping.

## Creating physics entities at runtime

To create a static collider entity at runtime:

1. Create an entity.  
2. Add the transform components, LocalTransform and LocalToWorld, to the entity.   
3. Add the rendering components to the entity by calling RenderMeshUtility.AddComponents.  
4. Set the RenderBounds component with an AABB that encompasses the object.  
5. Add a PhysicsWorldIndex component to the entity. (The default value of 0 is fine if you aren’t using more than one physics world.)  
6. Add a PhysicsCollider component to the entity. To create a BlobAssetReference\<Collider\> for the PhysicsCollider, you can use one of the convenience methods that return a collider:  
   * BoxCollider.Create  
   * CapsuleCollider.Create  
   * CompoundCollider.Create  
   * ConvexCollider.Create  
   * CylinderCollider.Create  
   * MeshCollider.Create   
   * PolygonCollider.CreateTriangle  
   * PolygonCollider.CreateQuad  
   * SphereCollider.Create  
   * TerrainCollider.Create

To create a dynamic body entity at runtime:

1. Follow the same steps as for a static collider.  
2. Add a PhysicsVelocity component to the entity.  
3. Add a PhysicsMass component to the entity. To create the PhysicsMass value, you can call PhysicsMass.CreateDynamic or PhysicsMass.CreateKinematic

*See a full code example [here](https://docs.unity3d.com/Packages/com.unity.physics@1.0/manual/create-body.html#creating-bodies-from-scratch).*

To create a joint or motor entity at runtime:

1. Create an entity.  
2. Add a PhysicsWorldIndex component to the entity. The default value of 0 is usually fine (unless you are using multiple physics worlds).  
3. Add the PhysicsConstrainedBodyPair component to the entity. Set the component to reference the two body entities.  
4. Add the PhysicsJoint component. There are a number of static methods for creating a PhysicsJoint with the appropriate settings:  
   * PhysicsJoint.CreateAngularVelocityMotor  
   * PhysicsJoint.CreateBallAndSocket  
   * PhysicsJoint.CreateFixed  
   * PhysicsJoint.CreateHinge  
   * PhysicsJoint.CreateLimitedDistance  
   * PhysicsJoint.CreateLimitedDOF  
   * PhysicsJoint.CreateLimitedHinge  
   * PhysicsJoint.CreateLinearVelocityMotor  
   * PhysicsJoint.CreatePositionMotor  
   * PhysicsJoint.CreatePrismatic  
   * PhysicsJoint.CreateRagdoll  
   * PhysicsJoint.CreateRotationalMotor

   **Note**: Manually setting the fields of the PhysicsJoint correctly requires deep understanding of the solver and so is not recommended for most users.

   

   Also note that, although the Built-in (GameObject PhysX) joint MonoBehaviours can be used to bake joint entities in subscenes, a few properties of these MonoBehaviours are ignored in baking because they do not map directly to attributes of a Unity Physics joint.

---

## The Physics systems

This is the hierarchy of physics-related system groups and systems (as of Unity Physics version 1.3.8):

* **Simulation System Group**  
  * **Fixed Step Simulation Group**  
    * Inject Temporal Coherence Data System  
    * **Physics System Group**  
      * Sync Custom Physics Proxy System  
      * Collider Blob Cleanup System  
      * **Before Physics System Group**  
        * Ensure Unique Collider System  
      * **Physics Initialize Group**  
        * Clean Physics Debug Data System\_Default  
        * **Physics Initialize Group Internal**  
          * Clean Physics World Dependency Resolver  
          * **Physics Build World Group**  
            * Inject Temporal Coherence Data Last Resort System  
            * Build Physics World  
            * Invalidated Temporal Coherence Cleanup System  
            * Integrity Check System  
          * Physics Analytics System  
      * **Physics Simulation Group**  
        * Physics Simulation Picker System  
        * **Physics Create Body Pairs Group**  
          * Broadphase System  
        * **Physics Create Contacts Group**  
          * Narrowphase System  
        * **Physics Create Jacobians Group**  
          * Create Jacobians System  
        * **Physics Solve And Integrate Group**  
          * Solve and Integrate System  
      * Buffer Interpolated Rigid Bodies Motion  
      * Record Most Recent Fixed Time  
      * Export Physics World  
      * **After Physics Simulation Group**  
        * Display Collision Events System  
        * Display Trigger Events System  
      * Modify Joint Limits System  
      * Copy Physics Velocity to Smoothing  
  * **Transform System Group**  
    * Smooth Rigid Bodies Graphical Motion  
  * **Late Simulation System Group**  
    * **Physics Debug Display Group**  
      * Display Body Colliders System  
      * Display Broadphase Aabbs System  
      * Display Collider Aabbs System\_Default  
      * Display Body Collider Edges\_Default  
      * Query Tester System  
      * Display Trigger Events System  
      * Display Mass Properties System  
      * Display Joints System\_Default  
      * Display Collision Events System  
      * Physics Debug Display System\_Default

  **Note**: When using Netcode for Entities, the server puts the PhysicsSystemGroup inside the PredictedFixedStepSimulationGroup.

The core physics update happens within each iteration of the PhysicsSystemGroup:

1. The PhysicsInitializeGroup sets up the “physics world”. This entails copying transforms, velocities, and other physics properties from the physics body entities to Unity Physics’s internal representation of the physics simulation.  
2. The BroadphaseSystem identifies the pairs of bodies that have overlapping AABB’s.  
3. The NarrowPhaseSystem determines which of these pairs actually collide and generates contacts accordingly.  
4. The CreateJacobiansSystem creates jacobians from the contacts and joint constraints.  
5. The SolveAndIntegrateSystem solves the jacobians to update the transforms and velocities within the physics world.  
6. The ExportPhysicsWorld system copies the transforms and velocities from the physics world back to the physics body entities.

Keep in mind that PhysicsSystemGroup is inside FixedStepSimulationGroup, which iterates 0 or more times each frame based on the fixed step rate, so 0 or more physics updates may be performed each frame.

**Note**: ExportPhysicsWorld expects the rigid bodies to remain in the same chunk location as found by the PhysicsInitializeGroup, so it is not safe in between these system updates to make structural changes that would move these entities or destroy them (*e.g.* do not add components or remove components from these entities). In the editor and development builds, ExportPhysicsWorld performs “integrity checks” to catch such mistakes, but the checks are skipped in release builds. Also understand that, because ExportPhysicsWorld overwrites the values of the physics components, any writes you make directly to the physics components in between PhysicsInitializeGroup and ExportPhysicsWorld will be clobbered and effectively ignored.

To order a system relative to the physics updates, you’ll typically use one of these attributes:

* \[UpdateBefore(typeof(FixedStepSimulationGroup))\]: Update once in the frame before all physics updates (even if none)  
* \[UpdateAfter(typeof(FixedStepSimulationGroup))\]:  Update once in the frame after all physics updates (even if none)  
* \[UpdateInGroup(typeof(BeforePhysicsSystemGroup))\]: Update immediately before each physics update of the frame (if any)  
* \[UpdateInGroup(typeof(AfterPhysicsSystemGroup))\]: Update immediately after each physics update of the frame (if any)