// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Physics;
namespace LibreLancer
{
    public class PhysicsComponent : GameComponent
    {
        public PhysicsObject Body;
        public float Mass; //0 mass means it can't move
        public Vector3? Inertia = null;
        public string SurPath;
        public float SphereRadius = -1;
        Collider collider;
        SurCollider sur;
        public uint PlainCrc = 0;
        PhysicsWorld pworld;

        public bool SetTransform = true;
        
        public PhysicsComponent(GameObject parent) : base(parent)
        {
        }
        public void ChildDebris(GameObject parent, RigidModelPart part, float mass, Vector3 initialforce)
        {
            var cp = new PhysicsComponent(parent) { 
                SurPath = this.SurPath,
                Mass = mass,
                PlainCrc = CrcTool.FLModelCrc(part.Name),
            };
            DisablePart(part);
            parent.PhysicsComponent = cp;
            cp.Register(pworld);
            cp.Body.Impulse(initialforce);
            parent.Components.Add(cp);
        }
        

        bool partRemoved = false;
        public void DisablePart(RigidModelPart part)
        {
            sur.RemovePart(part);
            partRemoved = true;
        }

        public override void FixedUpdate(double time)
        {
            if(partRemoved)
            {
                sur.FinishUpdatePart();
                partRemoved = true;
            }
            if (Body.Active && SetTransform)
            {
                Parent.SetLocalTransform(Body.Transform, true);
            }
            base.FixedUpdate(time);
        }

        public override void Register(PhysicsWorld physics)
        {
            if (pworld == physics) return;
            pworld = physics;
            Collider cld = null;
            if(SurPath == null) { //sphere
                cld = new SphereCollider(SphereRadius);
            } else {
                var mr = (ModelRenderer)Parent.RenderComponent;
                sur = new SurCollider(SurPath);
                cld = sur;
                if(Parent.RigidModel.From3db) {
                    sur.AddPart(PlainCrc, Matrix4x4.Identity, null);
                } else {
                    foreach(var part in Parent.RigidModel.AllParts) {
                        var crc = CrcTool.FLModelCrc(part.Name);
                        if (part.Construct == null)
                            sur.AddPart(crc, Matrix4x4.Identity, part);
                        else
                            sur.AddPart(crc, part.LocalTransform, part);
                    }
                }
            }
            if(Mass < float.Epsilon) {
                Body = physics.AddStaticObject(Parent.WorldTransform, cld);
            } else {
                Body = physics.AddDynamicObject(Mass, Parent.WorldTransform, cld, Inertia);
            }
            Body.Tag = Parent;
            collider = cld;
        }

        public void UpdateParts()
        {
            if (Parent.RigidModel == null) return;
            if (Body == null) return;
            foreach(var part in Parent.RigidModel.AllParts) {
                if (part.Construct != null)
                    sur.UpdatePart(part, part.LocalTransform);
            }
            sur.FinishUpdatePart();
        }
        public override void Unregister(PhysicsWorld physics)
        {
            pworld = null;
            physics.RemoveObject(Body);
            collider.Dispose();
        }
    }
}
