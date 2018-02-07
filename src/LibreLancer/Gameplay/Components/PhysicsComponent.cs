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
        }
        public override void Unregister(PhysicsWorld physics)
        {
            physics.RemoveObject(Body);
            collider.Dispose();
        }
    }
}
