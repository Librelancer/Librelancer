// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer.Utf.Cmp
{
    public abstract class HardpointDefinition
    {
        public string Name { get; private set; }
        public Matrix4x4 Orientation;
        public Vector3 Position;

        public HardpointDefinition(IntermediateNode root)
        {
            if (root == null) throw new ArgumentNullException("root");

            Name = root.Name;
			Orientation = Matrix4x4.Identity;
			Position = Vector3.Zero;
        }
        public HardpointDefinition(string name)
        {
            Name = name;
            Orientation = Matrix4x4.Identity;
            Position = Vector3.Zero;
        }
        protected bool parentNode(LeafNode node)
        {

            switch (node.Name.ToLowerInvariant())
            {
                case "orientation":
                    if (node.MatrixData3x3 != null)
                        Orientation = node.MatrixData3x3.Value;
                    else
                        FLLog.Error("3db", "Hardpoint " + Name + " has garbage orientation, defaulting to identity.");
                    break;
                case "position":
                    if (node.Vector3Data != null)
                        Position = node.Vector3Data.Value;
                    else
                        FLLog.Error("3db", "Hardpoint " + Name + " has garbage position, defaulting to zero.");
                    break;
                default:
                    return false;
            }

            return true;
        }

		public Transform3D Transform => new(Position, Orientation.ExtractRotation());
    }
}
