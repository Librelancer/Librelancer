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
        public PhysicsComponent(GameObject parent) : base(parent)
        {
        }

        public override void Register(PhysicsWorld physics)
        {
            Collider cld = null;
            if(SurPath == null) { //sphere
                cld = new SphereCollider(SphereRadius);
            } else {
                var mr = (ModelRenderer)Parent.RenderComponent;
                sur = new SurCollider(SurPath);
                cld = sur;
                if(mr.Model != null) {
                    sur.AddPart(0, Matrix4.Identity, null);
                } else {
                    foreach(var part in Parent.CmpParts) {
                        var crc = CrcTool.FLModelCrc(part.ObjectName);
                        if (part.Construct == null)
                            sur.AddPart(crc, Matrix4.Identity, null);
                        else
                            sur.AddPart(crc, part.Construct.Transform, part.Construct);
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
                    sur.UpdatePart(part.Construct, part.Construct.Transform);
            }
            sur.FinishUpdatePart();
        }
        public override void Unregister(PhysicsWorld physics)
        {
            physics.RemoveObject(Body);
            collider.Dispose();
        }
    }
}
