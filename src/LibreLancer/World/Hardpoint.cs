// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using LibreLancer.Render;
using LibreLancer.Utf.Cmp;

namespace LibreLancer.World
{
    public class Hardpoint : IRenderHardpoint
    {
        Matrix4x4 transform;
        public RigidModelPart Parent;
        public string Name;
        public RevoluteHardpointDefinition Revolute;
        public HardpointDefinition Definition;
        public float CurrentRevolution;
        Quaternion rotation = Quaternion.Identity;
        public void Revolve(float val)
        {
            var clamped = MathHelper.Clamp(val, Revolute.Min, Revolute.Max);
            CurrentRevolution = clamped;
            rotation = Quaternion.CreateFromAxisAngle(Revolute.Axis, clamped);
        }
        public Hardpoint(HardpointDefinition def, RigidModelPart parent)
        {
            Parent = parent;
            Definition = def;
            if(Definition == null) Definition = new FixedHardpointDefinition("dummy");
            Name = def == null ? "Dummy Hardpoint" : def.Name;
            Revolute = def as RevoluteHardpointDefinition;
        }

        public Transform3D HpTransformInfo
        {
            get {
                return Definition.Transform;
            }
        }
        public Transform3D TransformNoRotate
        {
            get
            {
                if (Parent != null)
                    return Definition.Transform * Parent.LocalTransform;
                else
                    return Definition.Transform;
            }
        }

        public Transform3D Transform
        {
            get
            {
                var tr = (new Transform3D(Vector3.Zero, rotation) * Definition.Transform);
                if (Parent != null)
                    return tr * Parent.LocalTransform;
                else
                    return tr;
            }
        }
        public override string ToString()
        {
            return string.Format("[{0}: {1}]", Name, Revolute != null ? "Rev" : "Fix");
        }
    }
}
