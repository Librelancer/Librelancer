// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using LibreLancer.Data;
using LibreLancer.Render;
using LibreLancer.Utf.Cmp;

namespace LibreLancer.World
{
    public class Hardpoint : IRenderHardpoint
    {
        public RigidModelPart? Parent;
        public HardpointDefinition Definition;
        public int ParentVersion = 0;

        public string Name
        {
            get => field;
            init
            {
                field = value;
                CRC = FLHash.CreateID(value);
            }
        }

        public uint CRC { get; private set; }

        public bool Revolute;
        public Vector3 RevolveAxis;
        public float RevolveMax;
        public float RevolveMin;
        public float CurrentRevolution;
        public Transform3D HpTransformInfo;

        private Quaternion rotation = Quaternion.Identity;
        private Transform3D rotatedTransform;

        public void Revolve(float val)
        {
            if (Revolute)
            {
                var clamped = MathHelper.Clamp(val, RevolveMin, RevolveMax);
                CurrentRevolution = clamped;
                rotation = Quaternion.CreateFromAxisAngle(RevolveAxis, clamped);
                rotatedTransform = new Transform3D(Vector3.Zero, rotation) * HpTransformInfo;
            }
        }

        public void RefreshValues()
        {
            HpTransformInfo = Definition.Transform;
            if (Definition is RevoluteHardpointDefinition rev)
            {
                Revolute = true;
                RevolveAxis = rev.Axis;
                RevolveMin = rev.Min;
                RevolveMax = rev.Max;
                Revolve(CurrentRevolution);
            }
            else
            {
                rotatedTransform = HpTransformInfo;
            }
        }

        public Hardpoint(HardpointDefinition def, RigidModelPart parent)
        {
            Parent = parent;
            Definition = def;
            Name = def.Name;
            HpTransformInfo = def.Transform;
            rotatedTransform = HpTransformInfo;
            if (def is RevoluteHardpointDefinition rev)
            {
                Revolute = true;
                RevolveAxis = rev.Axis;
                RevolveMin = rev.Min;
                RevolveMax = rev.Max;
            }
        }

        public Transform3D TransformNoRotate
        {
            get
            {
                if (Parent != null)
                    return HpTransformInfo * Parent.LocalTransform;
                else
                    return HpTransformInfo;
            }
        }

        public Transform3D Transform
        {
            get
            {
                if (Parent != null)
                    return rotatedTransform * Parent.LocalTransform;
                else
                    return rotatedTransform;
            }
        }
        public override string ToString()
        {
            return $"[{Name}: {(Revolute != null ? "Rev" : "Fix")}]";
        }
    }
}
