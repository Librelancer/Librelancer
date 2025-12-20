// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data;
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
        public CollisionMeshHandle SurPath;
        public float SphereRadius = -1;
        Collider collider;
        ConvexMeshCollider _convexMesh;
        public uint PlainCrc = 0;
        PhysicsWorld pworld;

        public bool SetTransform = true;

        public Vector3 PredictionErrorPos = Vector3.Zero;
        public Quaternion PredictionErrorQuat = Quaternion.Identity;
        public bool Collidable = true;

        public PhysicsComponent(GameObject parent) : base(parent)
        {
        }

        bool partRemoved = false;
        public void DisablePart(RigidModelPart part)
        {
            _convexMesh?.RemovePart(part);
            partRemoved = true;
        }

        List<Hardpoint> hardpoints = new List<Hardpoint>();

        public bool ActivateHardpoint(Hardpoint hardpoint)
        {
            var hpid = CrcTool.FLModelCrc(hardpoint.Name);
            var meshId = Parent.Model.RigidModel.Source == RigidModelSource.SinglePart
                ? 0
                : CrcTool.FLModelCrc(hardpoint.Parent.Name);
            hardpoints.Add(hardpoint);
            return _convexMesh.AddPart(SurPath.FileId, new ConvexMeshId(meshId, hpid), hardpoint.Parent.LocalTransform, hardpoint);
        }

        public void DeactivateHardpoint(Hardpoint hardpoint)
        {
            hardpoints.Remove(hardpoint);
            _convexMesh.RemovePart(hardpoint);
        }

        private bool isInterpolating = false;
        public void UpdateInterpolation(float fraction)
        {
            if (Body.Active && SetTransform)
            {
                var pos = Body.Position + PredictionErrorPos;
                var quat = Body.Orientation * PredictionErrorQuat;

                var interpPos = lastPosition + (pos - lastPosition) * fraction;
                var interpQuat = Quaternion.Slerp(lastOrientation, quat, fraction);
                Parent.SetLocalTransform(new Transform3D(interpPos, interpQuat), true);
            }
        }

        private Vector3 lastPosition;
        private Quaternion lastOrientation;

        public void SetOldTransform()
        {
            lastPosition = Body.Position + PredictionErrorPos;
            lastOrientation = Body.Orientation * PredictionErrorQuat;
        }


        public override void Update(double time)
        {
            if (Body == null) return;
            Body.Collidable = Collidable;
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
                var quat = Body.Orientation;

                Parent.SetLocalTransform(new Transform3D(pos + PredictionErrorPos, quat * PredictionErrorQuat), true);
            }
        }

        public override void Register(PhysicsWorld physics)
        {
            if (pworld == physics) return;
            pworld = physics;
            Collider cld = null;
            if(!SurPath.Valid)
            { //sphere
                cld = new SphereCollider(SphereRadius);
            }
            else
            {
                var meshId = SurPath.FileId;
                _convexMesh = new ConvexMeshCollider(physics);
                cld = _convexMesh;
                if(Parent.Model.RigidModel.Source == RigidModelSource.SinglePart)
                {
                    _convexMesh.AddPart(meshId, new ConvexMeshId(PlainCrc, 0), Transform3D.Identity, null);
                }
                else
                {
                    foreach(var part in Parent.Model.RigidModel.AllParts)
                    {
                        var crc = CrcTool.FLModelCrc(part.Name);
                        if (Parent.Model.IsPartDestroyed(crc))
                            continue;
                        var id = new ConvexMeshId(CrcTool.FLModelCrc(part.Name), 0);
                        if (part.Construct == null)
                            _convexMesh.AddPart( meshId, id, Transform3D.Identity, part);
                        else
                            _convexMesh.AddPart( meshId, id, part.LocalTransform, part);
                    }
                }
            }
            if (_convexMesh != null && _convexMesh.BepuChildCount == 0)
            {
                cld.Dispose();
                cld = new SphereCollider(1); //TODO: Bad
                FLLog.Error("Sur", $"Hull load failure for object {Parent.Nickname ?? Parent.NetID.ToString()}");
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
            if (Parent.Model == null) return;
            if (Body == null) return;
            foreach(var part in Parent.Model.RigidModel.AllParts) {
                if (part.Construct != null)
                    _convexMesh.UpdatePart(part, part.LocalTransform);
            }
            foreach (var hp in hardpoints)
            {
                if(hp.Parent.Construct != null)
                    _convexMesh.UpdatePart(hp, hp.Parent.LocalTransform);
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
