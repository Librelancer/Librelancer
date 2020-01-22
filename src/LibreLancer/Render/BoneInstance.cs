// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
namespace LibreLancer
{
    public class BoneInstance
    {
        public string Name;
        public BoneInstance Parent;
        public Matrix4 InvBindPose;
        public Matrix4 OriginalRotation = Matrix4.Identity;
        public Vector3 Origin = Vector3.Zero;
        public bool HasChildren = false;
        public Quaternion Rotation = Quaternion.Identity;
        public Vector3 Translation = Vector3.Zero;
        public List<BoneInstance> Children = new List<BoneInstance>();
        public void Update(Matrix4 parentMatrix)
        {
            var local = Matrix4.CreateFromQuaternion(Rotation) *
                        (OriginalRotation * Matrix4.CreateTranslation(Translation + Origin)) * parentMatrix;
            BoneMatrix = InvBindPose * local;
            for(int i = 0; i < Children.Count; i++)
                Children[i].Update(local);
        }
        public Matrix4 LocalTransform()
        {
            var mine = Matrix4.CreateFromQuaternion(Rotation) * (OriginalRotation * Matrix4.CreateTranslation(Translation + Origin));
            if (Parent != null)
                mine *= Parent.LocalTransform();
            return mine;
        }

        public Matrix4 BoneMatrix;
    }
}