using System;
using System.Numerics;
using BepuPhysics;
using BepuUtilities;

namespace LibreLancer.Physics;

//Taken from Demo Callbacks in Bepu

struct LibrelancerPoseIntegratorCallbacks : IPoseIntegratorCallbacks
{
    private Bodies bodies;
    public PhysicsWorld World;
    /// <summary>
    ///     Gets how the pose integrator should handle angular velocity integration.
    /// </summary>
    public readonly AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;

    /// <summary>
    ///     Gets whether the integrator should use substepping for unconstrained bodies when using a substepping solver.
    ///     If true, unconstrained bodies will be integrated with the same number of substeps as the constrained bodies in the
    ///     solver.
    ///     If false, unconstrained bodies use a single step of length equal to the dt provided to Simulation.Timestep.
    /// </summary>
    public readonly bool AllowSubstepsForUnconstrainedBodies => false;

    /// <summary>
    ///     Gets whether the velocity integration callback should be called for kinematic bodies.
    ///     If true, IntegrateVelocity will be called for bundles including kinematic bodies.
    ///     If false, kinematic bodies will just continue using whatever velocity they have set.
    ///     Most use cases should set this to false.
    /// </summary>
    public readonly bool IntegrateVelocityForKinematics => false;


    public void Initialize(Simulation simulation)
    {
        //In this demo, we don't need to initialize anything.
        //If you had a simulation with per body gravity stored in a CollidableProperty<T> or something similar, having the simulation provided in a callback can be helpful.
    }


    public void PrepareForIntegration(float dt)
    {
    }

    /// <summary>
    ///     Callback for a bundle of bodies being integrated.
    /// </summary>
    /// <param name="bodyIndices">Indices of the bodies being integrated in this bundle.</param>
    /// <param name="position">Current body positions.</param>
    /// <param name="orientation">Current body orientations.</param>
    /// <param name="localInertia">Body's current local inertia.</param>
    /// <param name="integrationMask">
    ///     Mask indicating which lanes are active in the bundle. Active lanes will contain
    ///     0xFFFFFFFF, inactive lanes will contain 0.
    /// </param>
    /// <param name="workerIndex">Index of the worker thread processing this bundle.</param>
    /// <param name="dt">Durations to integrate the velocity over. Can vary over lanes.</param>
    /// <param name="velocity">
    ///     Velocity of bodies in the bundle. Any changes to lanes which are not active by the
    ///     integrationMask will be discarded.
    /// </param>
    public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation,
        BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt,
        ref BodyVelocityWide velocity)
    {
        Span<float> linearDampingValues = stackalloc float[Vector<float>.Count];
        Span<float> angularDampingValues = stackalloc float[Vector<float>.Count];
        for (int bundleSlotIndex = 0; bundleSlotIndex < Vector<int>.Count; ++bundleSlotIndex)
        {
            var bodyIndex = bodyIndices[bundleSlotIndex];
            //Not every slot in the SIMD vector is guaranteed to be filled.
            if (bodyIndex >= 0)
            {
                var bodyHandle = World.Simulation.Bodies.ActiveSet.IndexToHandle[bodyIndex];
                linearDampingValues[bundleSlotIndex] = MathF.Pow(1.0f - World.dampings[bodyHandle].X, dt[bundleSlotIndex]);
                angularDampingValues[bundleSlotIndex] = MathF.Pow(1.0f - World.dampings[bodyHandle].Y, dt[bundleSlotIndex]);
            }
        }

        var linearDampingDt = new Vector<float>(linearDampingValues);
        var angularDampingDt = new Vector<float>(angularDampingValues);

        velocity.Linear *=  linearDampingDt;
        velocity.Angular *= angularDampingDt;
    }
}
