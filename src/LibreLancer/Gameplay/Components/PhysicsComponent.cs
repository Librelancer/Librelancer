// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
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
        uint plainCrc = 0;
        PhysicsWorld pworld;
        public PhysicsComponent(GameObject parent) : base(parent)
        {
        }
        public void ChildDebris(GameObject parent, Utf.Cmp.Part part, float mass, Vector3 initialforce)
        {
            var cp = new PhysicsComponent(parent) { 
                SurPath = this.SurPath,
                Mass = mass,
                plainCrc = CrcTool.FLModelCrc(part.ObjectName),
            };
            DisablePart(part);
            parent.PhysicsComponent = cp;
            cp.Register(pworld);
            cp.Body.Impulse(initialforce);
            parent.Components.Add(cp);
        }
            
        public void DisablePart(Utf.Cmp.Part part)
        {
            sur.RemovePart(part);
            sur.FinishUpdatePart();
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
                if(mr.Model != null) {
                    sur.AddPart(plainCrc, Matrix4.Identity, null);
                } else {
                    foreach(var part in Parent.CmpParts) {
                        var crc = CrcTool.FLModelCrc(part.ObjectName);
                        if (part.Construct == null)
                            sur.AddPart(crc, Matrix4.Identity, part);
                        else
                            sur.AddPart(crc, part.Construct.Transform, part);
                    }
                }
            }
            if(Mass < float.Epsilon) {
                Body = physics.AddStaticObject(Parent.GetTransform(), cld);
            } else {
                Body = physics.AddDynamicObject(Mass, Parent.GetTransform(), cld, Inertia);
            }
            Body.Tag = Parent;
            collider = cld;
        }

        public void UpdateParts()
        {
            if (Parent.CmpParts == null) return;
            if (Body == null) return;
            foreach(var part in Parent.CmpParts) {
                if (part.Construct != null)
                    sur.UpdatePart(part, part.Construct.Transform);
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
