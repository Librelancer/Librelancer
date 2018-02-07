/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Collections.Generic;
using BulletSharp;
using BM = BulletSharp.Math;
namespace LibreLancer.Physics
{
    public delegate void FixedUpdateHandler(TimeSpan elapsed);
    /// <summary>
    /// Creates a bullet physics world. Any object created here will be invalid upon Dispose()
    /// </summary>
    public class PhysicsWorld : IDisposable
    {
        public IReadOnlyList<PhysicsObject> Objects
        {
            get { return objects; }
        }

        public event FixedUpdateHandler FixedUpdate;

        DiscreteDynamicsWorld btWorld;
        CollisionDispatcher btDispatcher;
        DbvtBroadphase broadphase;
        CollisionConfiguration collisionConf;
        List<PhysicsObject> objects = new List<PhysicsObject>();
        List<PhysicsObject> dynamicObjects = new List<PhysicsObject>();
        bool disposed = false;

        public PhysicsWorld()
        {
            collisionConf = new DefaultCollisionConfiguration();

            btDispatcher = new CollisionDispatcher(collisionConf);
            broadphase = new DbvtBroadphase();
            btWorld = new DiscreteDynamicsWorld(btDispatcher, broadphase, null, collisionConf);
            btWorld.Gravity = BM.Vector3.Zero;
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
        public PhysicsObject AddStaticObject(Matrix4 transform, Collider col)
        {
            using(var rbInfo = new RigidBodyConstructionInfo(0, 
                                                             new DefaultMotionState(transform.Cast()), 
                                                             col.BtShape, BM.Vector3.Zero)) {
                var body = new RigidBody(rbInfo);
                body.Restitution = 1;
                var phys = new PhysicsObject(body, col) { Static = true };
                phys.UpdateProperties();
                body.UserObject = phys;
                btWorld.AddRigidBody(body);
                objects.Add(phys);
                return phys;
            }
        }

        public PhysicsObject AddDynamicObject(float mass, Matrix4 transform, Collider col, Vector3? inertia = null) {
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
                body.Restitution = 1;
                var phys = new PhysicsObject(body, col) { Static = false };
                phys.UpdateProperties();
                body.UserObject = phys;
                btWorld.AddRigidBody(body);
                objects.Add(phys);
                dynamicObjects.Add(phys);
                return phys;
            }
        }
        double accumulatedTime = 0;
        const float TIMESTEP = 1 / 60f;
        public void Step(TimeSpan elapsed)
        {
            if (disposed) throw new ObjectDisposedException("PhysicsWorld");
            accumulatedTime += elapsed.TotalSeconds;
            while(accumulatedTime > TIMESTEP) {
                FixedUpdate?.Invoke(TimeSpan.FromSeconds(TIMESTEP));
                if (disposed) return; //Alllow delete within FixedUpdate. Hacky but works
                btWorld.StepSimulation(TIMESTEP, 1, TIMESTEP);
                accumulatedTime -= TIMESTEP;
            }
            foreach(var obj in dynamicObjects) {
                obj.UpdateProperties();
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

            btWorld.Dispose();
            broadphase.Dispose();
            btDispatcher.Dispose();
            collisionConf.Dispose();
        }
    }
}
