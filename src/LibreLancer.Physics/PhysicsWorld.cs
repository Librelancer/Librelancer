// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using BulletSharp;
using BM = BulletSharp.Math;

namespace LibreLancer.Physics
{
    /// <summary>
    /// Creates a bullet physics world. Any object created here will be invalid upon Dispose()
    /// </summary>
    public class PhysicsWorld : IDisposable
    {
        public IReadOnlyList<PhysicsObject> Objects
        {
            get { return objects; }
        }
        
        DiscreteDynamicsWorld btWorld;
        CollisionDispatcher btDispatcher;
        DbvtBroadphase broadphase;
        CollisionConfiguration collisionConf;
        List<PhysicsObject> objects = new List<PhysicsObject>();
        List<PhysicsObject> dynamicObjects = new List<PhysicsObject>();
        bool disposed = false;
        
        private CollisionObject pointObj;

        public delegate void CollideHandler(PhysicsObject objA, PhysicsObject objB);

        public event CollideHandler OnCollision;

        public PhysicsWorld()
        {
            collisionConf = new DefaultCollisionConfiguration();

            btDispatcher = new CollisionDispatcher(collisionConf);
            broadphase = new DbvtBroadphase();
            btWorld = new DiscreteDynamicsWorld(btDispatcher, broadphase, null, collisionConf);
            btWorld.Gravity = BM.Vector3.Zero;

            pointObj = new CollisionObject();
            pointObj.CollisionShape = new SphereShape(1);
        }


        DebugDrawWrapper wrap;

        public void EnableWireframes(IDebugRenderer renderer)
        {
            wrap = new DebugDrawWrapper(renderer);
            btWorld.DebugDrawer = wrap;
        }

        public void DisableWireframes()
        {
            btWorld.DebugDrawer = null;
            wrap = null;
        }

        public void DrawWorld()
        {
            btWorld.DebugDrawWorld();
        }

        public PhysicsObject AddStaticObject(Matrix4x4 transform, Collider col)
        {
            using (var rbInfo = new RigidBodyConstructionInfo(0,
                new DefaultMotionState(transform.Cast()),
                col.BtShape, BM.Vector3.Zero))
            {
                var body = new RigidBody(rbInfo);
                body.Restitution = 1;
                var phys = new PhysicsObject(body, col) {Static = true};
                phys.UpdateProperties();
                body.UserObject = phys;
                btWorld.AddRigidBody(body);
                objects.Add(phys);
                return phys;
            }
        }

        static bool IsInvalid(BM.Vector3 v)
        {
            return float.IsNaN(v.X) ||
                float.IsNaN(v.Y) ||
                float.IsNaN(v.Z);
        }

        class SphereTestCallback : ContactResultCallback
        {
            public List<PhysicsObject> Result = new List<PhysicsObject>();

            public override float AddSingleResult(ManifoldPoint cp, CollisionObjectWrapper colObj0Wrap, int partId0, int index0,
                CollisionObjectWrapper colObj1Wrap, int partId1, int index1)
            {
                if (colObj0Wrap.CollisionObject is RigidBody rb0)
                {
                    if (rb0.UserObject != null)
                        Result.Add((PhysicsObject) rb0.UserObject);
                }
                else if (colObj1Wrap.CollisionObject is RigidBody rb1)
                {
                    if (rb1.UserObject != null)
                        Result.Add((PhysicsObject) rb1.UserObject);
                }
                return 1f;
            }
        }
        public List<PhysicsObject> SphereTest(Vector3 origin, float radius)
        {
            using var co = new CollisionObject();
            using var sph = new SphereShape(radius);
            using var cb = new SphereTestCallback();
            co.CollisionShape = sph;
            co.WorldTransform = BM.Matrix.Translation(origin.Cast());
            btWorld.ContactTest(co, cb);
            return cb.Result;
        }


        public bool PointRaycast(PhysicsObject me, Vector3 origin, Vector3 direction, float maxDist, out Vector3 contactPoint, out PhysicsObject didHit)
        {
            contactPoint = Vector3.Zero;
            didHit = null;
            ClosestRayResultCallback cb;
            var from = origin.Cast();
            var to = (origin + direction * maxDist).Cast();
            if (IsInvalid(from) || IsInvalid(to)) return false;
            if ((from - to).Length == 0) return false;

            if (me != null) {
                cb = new KinematicClosestNotMeRayResultCallback(me.RigidBody);
                cb.RayFromWorld = from;
                cb.RayToWorld = to;
            }
            else {
                cb = new ClosestRayResultCallback(ref from,  ref to);
            }
            using (cb)
            {
                btWorld.RayTestRef(ref from, ref to, cb);
                if (cb.HasHit)
                {
                    didHit = cb.CollisionObject.UserObject as PhysicsObject;
                    contactPoint = cb.HitPointWorld.Cast();
                    return true;
                }
                return false;
            }
        }

        public PhysicsObject AddDynamicObject(float mass, Matrix4x4 transform, Collider col, Vector3? inertia = null) {
            if(mass < float.Epsilon) {
                throw new Exception("Mass must be non-zero");
            }
            BM.Vector3 localInertia;
            if (inertia != null) {
                localInertia = inertia.Value.Cast();
            }
            else {
                col.BtShape.CalculateLocalInertia(mass, out localInertia);
            }
            using (var rbInfo = new RigidBodyConstructionInfo(mass,
                                                             new DefaultMotionState(transform.Cast()),
                                                             col.BtShape, localInertia))
            {
                var body = new RigidBody(rbInfo);
                body.SetDamping(0, 0);
                body.Restitution = 0.8f;
                var phys = new PhysicsObject(body, col) { Static = false };
                phys.UpdateProperties();
                body.UserObject = phys;
                btWorld.AddRigidBody(body);
                objects.Add(phys);
                dynamicObjects.Add(phys);
                return phys;
            }
        }

        public void StepSimulation(float timestep)
        {
            btWorld.StepSimulation(timestep, 0, timestep);
            if (disposed) return; //Allow delete within FixedUpdate. Hacky but works
            //Update C#-side properties after each step. Creates stuttering otherwise
            foreach (var obj in dynamicObjects) {
                obj.UpdateProperties();
                obj.RigidBody.Activate(true);
            }
            if (OnCollision != null)
            {
                int numManifolds = btDispatcher.NumManifolds;
                for (int i = 0; i < numManifolds; i++)
                {
                    var contactManifold = btDispatcher.GetManifoldByIndexInternal(i);
                    var numContacts = contactManifold.NumContacts;
                    for (int j = 0; j < numContacts; j++)
                    {
                        var point = contactManifold.GetContactPoint(j);
                        if (point.Distance < 0)
                        {
                            var body0 = contactManifold.Body0;
                            var body1 = contactManifold.Body1;
                            OnCollision(body0.UserObject as PhysicsObject, body1.UserObject as PhysicsObject);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Removes and destroys (!) the object
        /// </summary>
        /// <param name="obj">Physics Object to remove.</param>
        public void RemoveObject(PhysicsObject obj)
        {
            if (obj.RigidBody.MotionState != null)
                obj.RigidBody.MotionState.Dispose();
            btWorld.RemoveCollisionObject(obj.RigidBody);
            obj.RigidBody.Dispose();
            objects.Remove(obj);
            if (!obj.Static) dynamicObjects.Remove(obj);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            //Delete all RigidBody objects
            for (int i = btWorld.NumCollisionObjects - 1; i >= 0; i--)
            {
                CollisionObject obj = btWorld.CollisionObjectArray[i];
                RigidBody body = obj as RigidBody;
                if (body != null && body.MotionState != null)
                {
                    body.MotionState.Dispose();
                }
                btWorld.RemoveCollisionObject(obj);
                obj.Dispose();
            }
            pointObj.CollisionShape.Dispose();
            pointObj.Dispose();
            btWorld.Dispose();
            broadphase.Dispose();
            btDispatcher.Dispose();
            collisionConf.Dispose();
        }
    }
}
