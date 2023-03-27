// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using System.IO;

namespace LibreLancer.Sur
{
	public struct SurfacePoint : IEquatable<SurfacePoint>
    {
        public bool Equals(SurfacePoint other)
        {
            return Point.Equals(other.Point) && Mesh == other.Mesh;
        }

        public override bool Equals(object obj)
        {
            return obj is SurfacePoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Point, Mesh);
        }

        public static bool operator ==(SurfacePoint left, SurfacePoint right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SurfacePoint left, SurfacePoint right)
        {
            return !left.Equals(right);
        }

        private sealed class PointMeshEqualityComparer : IEqualityComparer<SurfacePoint>
        {
            public bool Equals(SurfacePoint x, SurfacePoint y)
            {
                return x.Point.Equals(y.Point) && x.Mesh == y.Mesh;
            }

            public int GetHashCode(SurfacePoint obj)
            {
                return HashCode.Combine(obj.Point, obj.Mesh);
            }
        }

        public static IEqualityComparer<SurfacePoint> PointMeshComparer { get; } = new PointMeshEqualityComparer();

        public Vector3 Point;
		public uint Mesh;

        public SurfacePoint(Vector3 point, uint mesh)
        {
            Mesh = mesh;
            Point = point;
        }

        public static SurfacePoint Read(BinaryReader reader)
        {
            return new SurfacePoint()
            {
                Point = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                Mesh = reader.ReadUInt32()
            };
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Point.X);
            writer.Write(Point.Y);
            writer.Write(Point.Z);
            writer.Write(Mesh);
        }
        
    }
}

