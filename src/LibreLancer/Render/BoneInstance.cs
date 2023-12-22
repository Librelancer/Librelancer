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
        public Matrix4x4 InvBindPose;
        public Matrix4x4 BoneMatrix = Matrix4x4.Identity;
        public Matrix4x4 OriginalRotation = Matrix4x4.Identity;
        public Vector3 Origin = Vector3.Zero;
        public Quaternion Rotation = Quaternion.Identity;
        public Vector3 Translation = Vector3.Zero;
        public List<BoneInstance> Children = new();
        public Matrix4x4 LocalTransform;

        public BoneInstance(string name, Matrix4x4 boneToRoot)
        {
            Name = name;
            LocalTransform = boneToRoot;
            Matrix4x4.Invert(boneToRoot, out InvBindPose);
        }

        public void Update(Matrix4x4 parentMatrix)
        {
            LocalTransform = Matrix4x4.CreateFromQuaternion(Rotation) *
                        (OriginalRotation * Matrix4x4.CreateTranslation(Translation + Origin)) * parentMatrix;
            BoneMatrix = InvBindPose * LocalTransform;
            foreach (var b in Children)
                b.Update(LocalTransform);
        }
    }
}
