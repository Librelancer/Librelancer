// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using System.Numerics;

namespace LibreLancer.Render
{
    public class BoneInstance
    {
        public string Name;
        public BoneInstance Parent;
        public Transform3D InvBindPose;
        public Matrix4x4 BoneMatrix = Matrix4x4.Identity;
        public Quaternion OriginalRotation = Quaternion.Identity;
        public Vector3 Origin = Vector3.Zero;
        public Quaternion Rotation = Quaternion.Identity;
        public Vector3 Translation = Vector3.Zero;
        public List<BoneInstance> Children = new();
        public Transform3D LocalTransform;
        public BoundingBox BoundingBox;

        private Vector3 bMin;
        private Vector3 bMax;

        public BoneInstance(string name, Transform3D boneToRoot, Vector3 bMin, Vector3 bMax)
        {
            Name = name;
            LocalTransform = boneToRoot;
            InvBindPose = boneToRoot.Inverse();
            this.bMin = InvBindPose.Transform(bMin);
            this.bMax = InvBindPose.Transform(bMax);
            BoundingBox = BoundingBox.TransformAABB(new BoundingBox(bMin, bMax), LocalTransform);
        }

        public void Update(Transform3D parentMatrix)
        {
            LocalTransform = new Transform3D(Vector3.Zero, Rotation) *
                             new Transform3D( Translation + Origin, OriginalRotation) * parentMatrix;
            BoneMatrix = (InvBindPose * LocalTransform).Matrix();
            foreach (var b in Children)
                b.Update(LocalTransform);
            BoundingBox = BoundingBox.TransformAABB(new BoundingBox(bMin, bMax), LocalTransform);
        }
    }
}
