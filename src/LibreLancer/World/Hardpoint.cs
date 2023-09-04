// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using LibreLancer.Utf.Cmp;

namespace LibreLancer.World
{
    public class Hardpoint
    {
        Matrix4x4 transform;
        public RigidModelPart Parent;
        public string Name;
        public RevoluteHardpointDefinition Revolute;
        public HardpointDefinition Definition;
        public float CurrentRevolution;
        Matrix4x4 rotation = Matrix4x4.Identity;
        public void Revolve(float val)
        {
            var clamped = MathHelper.Clamp(val, Revolute.Min, Revolute.Max);
            CurrentRevolution = clamped;
            rotation = Matrix4x4.CreateFromAxisAngle(Revolute.Axis, clamped);
        }
        public Hardpoint(HardpointDefinition def, RigidModelPart parent)
        {
            Parent = parent;
            Definition = def;
            if(Definition == null) Definition = new FixedHardpointDefinition("dummy");
            Name = def == null ? "Dummy Hardpoint" : def.Name;
            Revolute = def as RevoluteHardpointDefinition;
        }

        public Matrix4x4 HpTransformInfo
        {
            get {
                return Definition.Transform;
            }
        }
        public Matrix4x4 TransformNoRotate
        {
            get
            {
                if (Parent != null)
                    return Definition.Transform * Parent.LocalTransform;
                else
                    return Definition.Transform;
            }
        }

        public Matrix4x4 Transform
        {
            get
            {
                var tr = (rotation * Definition.Transform);
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
