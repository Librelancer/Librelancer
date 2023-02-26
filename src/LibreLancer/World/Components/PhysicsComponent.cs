// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using LibreLancer.Physics;
using LibreLancer.Render;
using LibreLancer.Sur;

namespace LibreLancer.World.Components
{
    public class PhysicsComponent : GameComponent
    {
        public PhysicsObject Body;
        public float Mass; //0 mass means it can't move
        public Vector3? Inertia = null;
        public SurFile Sur;
        public float SphereRadius = -1;
        Collider collider;
        ConvexMeshCollider _convexMesh;
        public uint PlainCrc = 0;
        PhysicsWorld pworld;

        public bool SetTransform = true;

        public Vector3 PredictionErrorPos = Vector3.Zero;
        public Quaternion PredictionErrorQuat = Quaternion.Identity;
        
        public PhysicsComponent(GameObject parent) : base(parent)
        {
        }
        public void ChildDebris(GameObject parent, RigidModelPart part, float mass, Vector3 initialforce)
        {
            var cp = new PhysicsComponent(parent) { 
                Sur = this.Sur,
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
            _convexMesh.RemovePart(part);
            partRemoved = true;
        }

        public override void Update(double time)
        {
            if (Body == null) return;
            if(partRemoved)
            {
                _convexMesh.FinishUpdatePart();
                partRemoved = true;
            }
            if (Body.Active && SetTransform)
            {
                //Smooth out errors
                if (PredictionErrorPos.Length() > 0 ||
                    MathHelper.QuatError(PredictionErrorQuat, Quaternion.Identity) > 0.001)
                {
                    PredictionErrorPos *= 0.95f;
                    if (PredictionErrorPos.Length() < 0.001) PredictionErrorPos = Vector3.Zero;
                    PredictionErrorQuat = Quaternion.Slerp(PredictionErrorQuat, Quaternion.Identity, 0.05f);
                    if(MathHelper.QuatError(PredictionErrorQuat, Quaternion.Identity) < 0.001)
                        PredictionErrorQuat = Quaternion.Identity;
                }
                var pos = Body.Position;
                var quat = Body.Transform.ExtractRotation();
                
                Parent.SetLocalTransform(Matrix4x4.CreateFromQuaternion(quat * PredictionErrorQuat) *
                    Matrix4x4.CreateTranslation(pos + PredictionErrorPos), true);
            }
        }

        public override void Register(PhysicsWorld physics)
        {
            if (pworld == physics) return;
            pworld = physics;
            Collider cld = null;
            if(Sur == null) { //sphere
                cld = new SphereCollider(SphereRadius);
            } else {
                var mr = (ModelRenderer)Parent.RenderComponent;
                _convexMesh = new ConvexMeshCollider(Sur);
                cld = _convexMesh;
                if(Parent.RigidModel.Source == RigidModelSource.SinglePart) {
                    _convexMesh.AddPart(PlainCrc, Matrix4x4.Identity, null);
                } else {
                    foreach(var part in Parent.RigidModel.AllParts) {
                        var crc = CrcTool.FLModelCrc(part.Name);
                        if (part.Construct == null)
                            _convexMesh.AddPart(crc, Matrix4x4.Identity, part);
                        else
                            _convexMesh.AddPart(crc, part.LocalTransform, part);
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
                    _convexMesh.UpdatePart(part, part.LocalTransform);
            }
            _convexMesh.FinishUpdatePart();
        }
        public override void Unregister(PhysicsWorld physics)
        {
            pworld = null;
            physics.RemoveObject(Body);
            collider.Dispose();
        }
    }
}
