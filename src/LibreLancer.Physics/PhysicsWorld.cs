// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Trees;
using BepuUtilities;
using BepuUtilities.Memory;
using LibreLancer.Physics.ContactEvents;

namespace LibreLancer.Physics
{
    /// <summary>
    /// Creates a bullet physics world. Any object created here will be invalid upon Dispose()
    /// </summary>
    public class PhysicsWorld : IDisposable, ContactEvents.IContactEventHandler
    {
        public IReadOnlyList<PhysicsObject> Objects
        {
            get { return objects; }
        }

        //mapipng bepu bodies to librelancer objects
        private Dictionary<int, PhysicsObject> objectsById = new Dictionary<int, PhysicsObject>();
        private IdPool ids = new IdPool(100, true);
        private CollidableProperty<int> bepuToLancer;
        internal CollidableProperty<bool> collidableObjects;
        //our list
        List<PhysicsObject> objects = new List<PhysicsObject>();
        List<PhysicsObject> dynamicObjects = new List<PhysicsObject>();

        public List<PhysicsObject> DynamicObjects => dynamicObjects;


        bool disposed = false;

        public BufferPool BufferPool;
        private ThreadDispatcher threadDispatcher;
        internal Simulation Simulation;
        private ContactEvents.ContactEvents contactEvents;

        public delegate void CollideHandler(PhysicsObject objA, PhysicsObject objB);

        public event CollideHandler OnCollision;

        public PhysicsWorld()
        {
            BufferPool = new BufferPool();
            threadDispatcher = new ThreadDispatcher(Math.Clamp(Environment.ProcessorCount / 2, 1, 8));
            contactEvents = new ContactEvents.ContactEvents(threadDispatcher, BufferPool);
            Simulation = Simulation.Create(BufferPool,
                new ContactEventCallbacks(contactEvents, this, 300),
                new LibrelancerPoseIntegratorCallbacks(),
                new SolveDescription(8, 1)
            );
            bepuToLancer = new CollidableProperty<int>(Simulation);
            collidableObjects = new CollidableProperty<bool>(Simulation);
            objectsById[-1] = null;
        }

        public IDebugRenderer DebugRenderer { get; set; }

        public void DrawWorld(ICamera camera)
        {
            var position = camera.Position;
            if (DebugRenderer != null)
            {
                foreach (var o in objects)
                {
                    if (Vector3.DistanceSquared(o.Position, position) > (25000 * 25000))
                        continue;
                    if (!camera.FrustumCheck(o.GetBoundingBox()))
                        continue;
                    o.Collider.Draw(o.Transform, DebugRenderer);
                }
                if (ShowRaycasts)
                {
                    for (int i = 0; i < debugRayIndex; i++)
                    {
                        DebugRenderer.DrawLine(debugRays[i].Start, debugRays[i].End, debugRays[i].Success ? Color4.Green : Color4.Blue);
                        if (debugRays[i].Success)
                        {
                            DebugRenderer.DrawLine(debugRays[i].End - new Vector3(0,10,0), debugRays[i].End + new Vector3(0,10,0), Color4.Red);
                            DebugRenderer.DrawLine(debugRays[i].End - new Vector3(10,0,0), debugRays[i].End + new Vector3(10,0,0), Color4.Red);
                            DebugRenderer.DrawLine(debugRays[i].End - new Vector3(0,0,10), debugRays[i].End + new Vector3(0,0,10), Color4.Red);
                        }
                    }
                }
            }
            debugRayIndex = 0;
        }

        struct DisallowAwakening : IStaticChangeAwakeningFilter
        {
            public bool AllowAwakening => false;
            public bool ShouldAwaken(BodyReference body) => false;
        }

        public PhysicsObject AddStaticObject(Matrix4x4 transform, Collider col)
        {
            col.Create(Simulation, BufferPool);
            var h = Simulation.Statics.Add(new StaticDescription(transform.ToPose(), col.Handle));
            ids.TryAllocate(out int id);
            var obj = new StaticObject(id, Simulation.Statics.GetStaticReference(h), this, transform, col);
            bepuToLancer.Allocate(h) = id;
            collidableObjects.Allocate(h) = true;
            objectsById[id] = obj;
            objects.Add(obj);
            return obj;
        }



        public void CreateUnmanagedStatic(ref UnmanagedStatic obj, Matrix4x4 transform, Collider col)
        {
            if (obj.Valid)
                throw new InvalidOperationException("Object is already created");
            col.Create(Simulation, BufferPool);
            var disallow = new DisallowAwakening();
            var h = Simulation.Statics.Add(new StaticDescription(transform.ToPose(), col.Handle), ref disallow);
            bepuToLancer.Allocate(h) = -1;
            collidableObjects.Allocate(h) = true;
            obj.Valid = true;
            obj.Handle = h;
        }

        public void RemoveUnmanagedStatic(ref UnmanagedStatic obj)
        {
            if (!obj.Valid)
                throw new InvalidOperationException("Object does not exist");
            Simulation.Statics.Remove(obj.Handle);
            obj = default;
        }

       struct SweepHandler : ISweepHitHandler
       {
           private PhysicsWorld world;
           public List<PhysicsObject> Result;

           public SweepHandler(PhysicsWorld world)
           {
               this.world = world;
               Result = new List<PhysicsObject>();
           }

           public bool AllowTest(CollidableReference collidable) => world.collidableObjects[collidable];

           public bool AllowTest(CollidableReference collidable, int child) => world.collidableObjects[collidable];

           public void OnHit(ref float maximumT, float t, Vector3 hitLocation, Vector3 hitNormal, CollidableReference collidable)
           {
               Result.Add(world.objectsById[world.bepuToLancer[collidable]]);
           }

           public void OnHitAtZeroT(ref float maximumT, CollidableReference collidable)
           {
               Result.Add(world.objectsById[world.bepuToLancer[collidable]]);
           }
       }

       //TODO: Use a CollisionQuery instead.
        public List<PhysicsObject> SphereTest(Vector3 origin, float radius)
        {
            SweepHandler handler = new SweepHandler(this);
            Simulation.Sweep(
                new Sphere(radius),
                new RigidPose(origin),
                new BodyVelocity(Vector3.Zero),
                1,
                BufferPool,
                ref handler
            );
            return handler.Result;
        }

        struct HitHandler : IRayHitHandler
        {
            public PhysicsObject Result;
            public Vector3 ContactPoint;
            private PhysicsWorld world;
            private int selfId;
            public bool DidHit;

            public HitHandler(PhysicsWorld world, PhysicsObject self)
            {
                this.world = world;
                selfId = self?.Id ?? -1;
            }

            public bool AllowTest(CollidableReference collidable) =>
                world.bepuToLancer[collidable] != selfId &&
                                                                     world.collidableObjects[collidable];

            public bool AllowTest(CollidableReference collidable, int childIndex) =>
                world.bepuToLancer[collidable] != selfId &&
                world.collidableObjects[collidable];

            public void OnRayHit(in RayData ray, ref float maximumT, float t, Vector3 normal, CollidableReference collidable,
                int childIndex)
            {
                maximumT = t;
                ContactPoint = ray.Origin + ray.Direction * t;
                Result = world.objectsById[world.bepuToLancer[collidable]];
                DidHit = true;
            }
        }

        private int debugRayIndex = 0;
        public bool ShowRaycasts = false;
        private (Vector3 Start, Vector3 End, bool Success)[] debugRays = new (Vector3 Start, Vector3 End, bool Success)[32];

        public bool PointRaycast(PhysicsObject me, Vector3 origin, Vector3 direction, float maxDist, out Vector3 contactPoint, out PhysicsObject didHit)
        {
            HitHandler handler = new HitHandler(this, me);
            Simulation.RayCast(origin, direction, maxDist, ref handler);
            contactPoint = handler.ContactPoint;
            didHit = handler.Result;
            if (ShowRaycasts && debugRayIndex < debugRays.Length){
                if (handler.DidHit)
                {
                    debugRays[debugRayIndex++] = (origin, contactPoint, true);
                }
                else
                {
                    debugRays[debugRayIndex++] = (origin, origin + (direction * maxDist), false);
                }
            }
            return handler.DidHit;
        }


        //TODO : Not good
        public uint UseMeshFile(IConvexMeshProvider file)
        {
            if (meshFileIds.TryGetValue(file, out var id))
                return id;
            nextMeshId++;
            meshFiles[nextMeshId] = file;
            meshFileIds[file] = nextMeshId;
            return nextMeshId;
        }

        private Dictionary<ulong, (TypedIndex Shape, Vector3 Center)[]> shapes = new();
        private Dictionary<uint, IConvexMeshProvider> meshFiles = new();
        private Dictionary<IConvexMeshProvider, uint> meshFileIds = new();
        private uint nextMeshId = 0;
        internal (TypedIndex Shape, Vector3 Center)[] GetConvexShapes(uint fileId, uint meshId)
        {
            var id = (ulong) meshId | ((ulong) fileId << 32);
            if (shapes.TryGetValue(id, out var sh))
                return sh;
            var f = meshFiles[fileId];
            var src = f.GetMesh(meshId);
            var shx = new List<(TypedIndex Shapes, Vector3 Center)>();
            for (int i = 0; i < src.Length; i++)
            {
                try
                {
                    var verts = src[i].Vertices;
                    var indices = src[i].Indices;
                    if (indices.Length <= 6) {
                        //Two triangles does not a convex hull make
                        if (indices.Length >= 3)
                        {
                            var tri = new Triangle(
                                verts[indices[0]],
                                verts[indices[1]],
                                verts[indices[2]]
                            );
                            shx.Add((Simulation.Shapes.Add(tri), Vector3.Zero));
                        }
                        if (indices.Length == 6){
                            var tri = new Triangle(
                                verts[indices[3]],
                                verts[indices[4]],
                                verts[indices[5]]
                            );
                            shx.Add((Simulation.Shapes.Add(tri), Vector3.Zero));
                        }
                    }
                    else
                    {
                        var points = new Vector3[src[i].Indices.Length];
                        for (int j = 0; j < src[i].Indices.Length; j++)
                            points[j] = src[i].Vertices[src[i].Indices[j]];
                        var convexHull = new ConvexHull(points, BufferPool, out var center);
                        if (convexHull.FaceToVertexIndicesStart.Length == 2)
                        {
                            //Co-planar, fix up
                            convexHull.Dispose(BufferPool);
                            for (int j = 0; j < indices.Length; j += 3)
                            {
                                shx.Add((Simulation.Shapes.Add(new Triangle(
                                    verts[indices[j]],
                                    verts[indices[j+1]],
                                    verts[indices[j+2]]
                                    )), Vector3.Zero));
                            }
                        }
                        else
                        {
                            shx.Add((Simulation.Shapes.Add(convexHull), center));
                        }
                    }
                }
                catch (Exception e)
                {
                    FLLog.Error("Physics", $"Please report: {e}");
                }
            }

            shapes[id] = shx.ToArray();
            return shapes[id];
        }

        static Symmetric3x3 ToInverseInertia(Vector3 inertia)
        {
            Vector3 inverted = new Vector3(
                inertia.X == 0 ? 0 : 1.0f / inertia.X,
                inertia.Y == 0 ? 0 : 1.0f / inertia.Y,
                inertia.Z == 0 ? 0 : 1.0f / inertia.Z
            );
            return new Symmetric3x3()
            {
                XX = inverted.X,
                YY = inverted.Y,
                ZZ = inverted.Z
            };
        }

        public PhysicsObject AddDynamicObject(float mass, Matrix4x4 transform, Collider col, Vector3? inertia = null)
        {
            if(mass < float.Epsilon) {
                throw new Exception("Mass must be non-zero");
            }
            Symmetric3x3 invInertia;
            if (inertia != null)
                invInertia = ToInverseInertia(inertia.Value);
            else
                invInertia = col.CalculateInverseInertia(mass);
            col.Create(Simulation, BufferPool);
            var h = Simulation.Bodies.Add(new BodyDescription()
            {
                LocalInertia = new BodyInertia()
                {
                    InverseMass = 1.0f / mass,
                    InverseInertiaTensor = invInertia,
                },
                Collidable = new CollidableDescription(col.Handle),
                Pose = transform.ToPose(),
            });
            ids.TryAllocate(out int id);
            var obj = new DynamicObject(id, this, Simulation.Bodies.GetBodyReference(h), col);
            bepuToLancer.Allocate(h) = id;
            collidableObjects.Allocate(h) = true;
            objectsById[id] = obj;
            objects.Add(obj);
            dynamicObjects.Add(obj);
            contactEvents.Register(h, this);
            return obj;
        }

        void ContactEvents.IContactEventHandler.OnStartedTouching<TManifold>(CollidableReference eventSource, CollidablePair pair,
            ref TManifold contactManifold, int workerIndex)
        {
            QueueCollision(pair.A, pair.B);
        }

        private ConcurrentQueue<(PhysicsObject A, PhysicsObject B)> collisionEvents =
            new ConcurrentQueue<(PhysicsObject A, PhysicsObject B)>();

        internal void QueueCollision(CollidableReference a, CollidableReference b)
        {
            var objA = objectsById[bepuToLancer[a]];
            var objB = objectsById[bepuToLancer[b]];
            collisionEvents.Enqueue((objA, objB));
        }

        public void StepSimulation(float timestep)
        {
            if (timestep < float.Epsilon) // We're paused
                return;
            Simulation.Timestep(timestep, threadDispatcher);
            foreach(var obj in objects)
                obj.UpdateProperties();
            contactEvents.Flush();
            while (collisionEvents.TryDequeue(out var ev))
            {
                OnCollision?.Invoke(ev.A, ev.B);
            }
        }

        /// <summary>
        /// Removes and destroys (!) the object
        /// </summary>
        /// <param name="obj">Physics Object to remove.</param>
        public void RemoveObject(PhysicsObject obj)
        {
            if (!objects.Remove(obj))
                throw new InvalidOperationException("Object already freed");
            int id = -1;
            if (obj is StaticObject s)
            {
                id = bepuToLancer[s.BepuObject.Handle];
                Simulation.Statics.Remove(s.BepuObject.Handle);
            }
            else if (obj is DynamicObject d)
            {
                id = bepuToLancer[d.BepuObject.Handle];
                contactEvents.Unregister(d.BepuObject.Handle);
                Simulation.Bodies.Remove(d.BepuObject.Handle);
                dynamicObjects.Remove(d);
            }
            if (id != -1)
            {
                ids.Free(id);
                objectsById.Remove(id);
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            contactEvents.Dispose();
            Simulation.Dispose();
            threadDispatcher.Dispose();
            BufferPool.Clear();
        }
    }
}
