// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using LibreLancer.Data.Schema.Universe;

namespace LibreLancer.Data.GameData.World
{
    // Shape Sizes
    // Sphere: X = radius
    // Box: XYZ = size
    // Ellipsoid: XYZ = size
    // Cylinder: X = radius, Y = height
    // Ring: X = outer radius, Y = height, Z = inner radius
	public class Zone
	{
        //Used for fast comparisons
        struct ZoneData
        {
            public int IdsName;
            public int IdsInfo;
            public Vector3 Position;
            public ShapeKind Shape;
            public Vector3 Size;
            public Matrix4x4 RotationMatrix;
            public Vector3 RotationAngles;
            public float EdgeFraction;
            public ZonePropFlags PropertyFlags;
            public Color4? PropertyFogColor;
            public float Sort;
            public VisitFlags VisitFlags;
            public int SpacedustMaxParticles;
            public float Interference;
            public float PowerModifier;
            public float DragModifier;
            public int LaneId;
            public int TradelaneAttack;
            public int TradelaneDown;
            public float Damage;
            public int Toughness;
            public int Density;
            public bool? PopulationAdditive;
            public bool MissionEligible;
            public int MaxBattleSize;
            public int ReliefTime;
            public int RepopTime;
        }

        struct BoxData
        {
            public Matrix4x4 R;
            public Vector3 P;
        }

        struct CylRingData
        {
            public Vector3 P1;
            public Vector3 P2;
            public Vector3 SZ2;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct ShapeData
        {
            [FieldOffset(0)] public BoxData Box;
            [FieldOffset(0)] public CylRingData CylRing;
        }

        private ShapeData _shape;
        private ZoneData data;

        void UpdateShape(bool isPosition = false)
        {
            switch (data.Shape)
            {
                case ShapeKind.Box:
                case ShapeKind.Ellipsoid:
                    _shape.Box.R = Matrix4x4.Transpose(data.RotationMatrix);
                    _shape.Box.P = Vector3.Transform(data.Position, _shape.Box.R);
                    break;
                case ShapeKind.Ring:
                case ShapeKind.Cylinder:
                    _shape.CylRing.P1 = Vector3.Transform(data.Position - new Vector3(0, data.Size.Y * 0.5f, 0),
                        data.RotationMatrix);
                    _shape.CylRing.P2 = Vector3.Transform(data.Position + new Vector3(0, data.Size.Y * 0.5f, 0),
                        data.RotationMatrix);
                    _shape.CylRing.SZ2 = data.Size * data.Size;
                    break;
            }

            if (!isPosition)
            {
                topDown = null;
                outline = null;
            }
        }

        private static bool _scaledErrorTriggered = false;
        public float ScaledDistance(Vector3 point)
        {
            switch (data.Shape)
            {
                case ShapeKind.Sphere:
                    return Vector3.Distance(data.Position, point) / data.Size.X;
                case ShapeKind.Box:
                {
                    //Not really right
                    var max = Math.Max(Math.Max(data.Size.X, data.Size.Y), data.Size.Z);
                    return Vector3.Distance(data.Position, point) / max;
                }
                case ShapeKind.Ellipsoid:
                {
                    //Transform point
                    point = Vector3.Transform(point, _shape.Box.R) - _shape.Box.P;
                    //Test
                    return PrimitiveMath.EllipsoidFunction(Vector3.Zero, data.Size, point);
                }
                //We don't support cylinder/ring scaled distance
                default:
                    if (!_scaledErrorTriggered)
                    {
                        FLLog.Error("Zones", $"Fixme: Scaled distance called on {data.Shape} ({Nickname})");
                        _scaledErrorTriggered = true;
                    }
                    // BAD
                    return Vector3.Distance(data.Position, point) / data.Size.X;
            }
        }

        public bool Intersects(BoundingBox box)
        {
            switch(data.Shape)
            {
                case ShapeKind.Box:
                {
                    var min = data.Position - (data.Size / 2);
                    var max = data.Position + (data.Size / 2);
                    var me = new BoundingBox(min, max);
                    return me.Intersects(box);
                }
                case ShapeKind.Sphere:
                {
                    var me = new BoundingSphere(data.Position, data.Size.X);
                    return box.Intersects(me);
                }
                case ShapeKind.Ellipsoid:
                {
                    //This is not correct, but good enough for now
                    Span<Vector3> corners = stackalloc Vector3[8];
                    box.GetCorners(corners);
                    foreach (var c in corners)
                    {
                        if (ContainsPoint(c))
                            return true;
                    }
                    return false;
                }
                //We don't support cylinder/ring bbox intersection
                default:
                    throw new InvalidOperationException();
            }
        }

        public bool ContainsPoint(Vector3 point)
        {
            switch (data.Shape)
            {
                case ShapeKind.Sphere:
                    return Vector3.Distance(data.Position, point) <= data.Size.X;
                case ShapeKind.Box:
                {
                    point = Vector3.Transform(point,_shape.Box.R) - _shape.Box.P;
                    //test
                    var max = (data.Size * 0.5f);
                    var min = -max;
                    return !(point.X < min.X || point.Y < min.Y || point.Z < min.Z || point.X > max.X || point.Y > max.Y || point.Z > max.Z);
                }
                case ShapeKind.Ellipsoid:
                {
                    point = Vector3.Transform(point,_shape.Box.R) - _shape.Box.P;
                    return PrimitiveMath.EllipsoidContains(Vector3.Zero, data.Size, point);
                }
                case ShapeKind.Cylinder:
                {
                    Vector3 d = _shape.CylRing.P2 - _shape.CylRing.P1;
                    Vector3 pd = point - _shape.CylRing.P1;
                    float dot = Vector3.Dot (pd, d);

                    if (dot < 0.0f || dot > _shape.CylRing.SZ2.Y)
                    {
                        return false;
                    }
                    else
                    {
                        float dsq = (pd.X * pd.X + pd.Y * pd.Y + pd.Z * pd.Z) - dot * dot / _shape.CylRing.SZ2.Y;
                        return dsq <= _shape.CylRing.SZ2.X;
                    }
                }
                case ShapeKind.Ring:
                {
                    Vector3 d = _shape.CylRing.P2 - _shape.CylRing.P1;
                    Vector3 pd = point - _shape.CylRing.P1;
                    float dot = Vector3.Dot (pd, d);

                    if (dot < 0.0f || dot > _shape.CylRing.SZ2.Y)
                    {
                        return false;
                    }
                    else
                    {
                        float dsq = (pd.X * pd.X + pd.Y * pd.Y + pd.Z * pd.Z) - dot * dot / _shape.CylRing.SZ2.Y;
                        return dsq <= _shape.CylRing.SZ2.X && dsq >= _shape.CylRing.SZ2.Z;
                    }
                }
                default:
                    throw new InvalidOperationException();
            }
        }

        static bool StructsEqual<T>(ref T a, ref T b) where T : unmanaged
        {
            var x = MemoryMarshal.Cast<T, byte>(MemoryMarshal.CreateSpan(ref a, 1));
            var y = MemoryMarshal.Cast<T, byte>(MemoryMarshal.CreateSpan(ref b, 1));
            return x.SequenceEqual(y);
        }

        static bool ArraysEqual<T>(T[] a, T[] b)
        {
            if (a == b) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            return a.AsSpan().SequenceEqual(b);
        }

		public string Nickname;
        public ref int IdsName => ref data.IdsName;
        public ref int IdsInfo => ref data.IdsInfo;

        public Vector3 Position
        {
            get => data.Position;
            set
            {
                data.Position = value;
                UpdateShape(true);
            }
        }

        public Matrix4x4 RotationMatrix
        {
            get => data.RotationMatrix;
            set
            {
                data.RotationMatrix = value;
                UpdateShape();
            }
        }

        public Vector3 Size
        {
            get => data.Size;
            set
            {
                data.Size = value;
                UpdateShape();
            }
        }

        public ShapeKind Shape
        {
            get => data.Shape;
            set
            {
                data.Shape = value;
                UpdateShape();
            }
        }

        private Vector2[] topDown = null;
        private Vector2[] outline = null;

        private static readonly Vector3[] _cubeVertices =
        {
            new(-0.5f, -0.5f, -0.5f),
            new(0.5f, -0.5f, -0.5f),
            new(0.5f, -0.5f, 0.5f),
            new(-0.5f, -0.5f, 0.5f),
            new(-0.5f, 0.5f, -0.5f),
            new(0.5f, 0.5f, -0.5f),
            new(0.5f, 0.5f, 0.5f),
            new(-0.5f, 0.5f, 0.5f)
        };

        private static readonly Vector3[] _cylinderVertices =
        {
            new(0.0f, 0.5f, 0.0f),
            new(0.0f, -0.5f, 0.0f),
            new(1.0f, 0.5f, 0.0f),
            new(1.0f, -0.5f, 0.0f),
            new(0.9511f, 0.5f, 0.3090f),
            new(0.9511f, -0.5f, 0.3090f),
            new(0.8090f, 0.5f, 0.5878f),
            new(0.8090f, -0.5f, 0.5878f),
            new(0.5878f, 0.5f, 0.8090f),
            new(0.5878f, -0.5f, 0.8090f),
            new(0.3090f, 0.5f, 0.9511f),
            new(0.3090f, -0.5f, 0.9511f),
            new(0.0f, 0.5f, 1.0f),
            new(0.0f, -0.5f, 1.0f),
            new(-0.3090f, 0.5f, 0.9511f),
            new(-0.3090f, -0.5f, 0.9511f),
            new(-0.5878f, 0.5f, 0.8090f),
            new(-0.5878f, -0.5f, 0.8090f),
            new(-0.8090f, 0.5f, 0.5878f),
            new(-0.8090f, -0.5f, 0.5878f),
            new(-0.9511f, 0.5f, 0.3090f),
            new(-0.9511f, -0.5f, 0.3090f),
            new(-1.0f, 0.5f, 0.0f),
            new(-1.0f, -0.5f, 0.0f),
            new(-0.9511f, 0.5f, -0.3090f),
            new(-0.9511f, -0.5f, -0.3090f),
            new(-0.8090f, 0.5f, -0.5878f),
            new(-0.8090f, -0.5f, -0.5878f),
            new(-0.5878f, 0.5f, -0.8090f),
            new(-0.5878f, -0.5f, -0.8090f),
            new(-0.3090f, 0.5f, -0.9511f),
            new(-0.3090f, -0.5f, -0.9511f),
            new(0.0f, 0.5f, -1.0f),
            new(0.0f, -0.5f, -1.0f),
            new(0.3090f, 0.5f, -0.9511f),
            new(0.3090f, -0.5f, -0.9511f),
            new(0.5878f, 0.5f, -0.8090f),
            new(0.5878f, -0.5f, -0.8090f),
            new(0.8090f, 0.5f, -0.5878f),
            new(0.8090f, -0.5f, -0.5878f),
            new(0.9511f, 0.5f, -0.3090f),
            new(0.9511f, -0.5f, -0.3090f)
        };

        private const int SECTORS = 48;

        public Vector2[] OutlineMesh()
        {
            if (outline != null)
                return outline;

            if (Shape == ShapeKind.Ellipsoid)
            {
                outline = new Vector2[(SECTORS - 1) * 2];
                int j = 0;
                for (int i = 1; i < SECTORS; i++)
                {
                    var lastT = ((2 * MathF.PI) / (SECTORS - 1)) * (i - 1);
                    var t = ((2 * MathF.PI) / (SECTORS - 1)) * i;
                    var p0 = Vector3.Transform(PrimitiveMath.GetPointOnRadius(Size, 0, lastT), RotationMatrix);
                    var p1 = Vector3.Transform(PrimitiveMath.GetPointOnRadius(Size, 0, t), RotationMatrix);
                    outline[j++] = new Vector2(p0.X, p0.Z);
                    outline[j++] = new Vector2(p1.X, p1.Z);
                }
            }
            else if (Shape == ShapeKind.Sphere)
            {
                // Don't rotate for sphere
                outline = new Vector2[(SECTORS - 1) * 2];
                int j = 0;
                var sz = new Vector3(Size.X);
                for (int i = 1; i < SECTORS; i++)
                {
                    var lastT = ((2 * MathF.PI) / (SECTORS - 1)) * (i - 1);
                    var t = ((2 * MathF.PI) / (SECTORS - 1)) * i;
                    var p0 = PrimitiveMath.GetPointOnRadius(sz, 0, lastT);
                    var p1 = PrimitiveMath.GetPointOnRadius(sz, 0, t);
                    outline[j++] = new Vector2(p0.X, p0.Z);
                    outline[j++] = new Vector2(p1.X, p1.Z);
                }
            }
            else if (Shape == ShapeKind.Box)
            {
                //Flatten a mesh + create convex hull
                List<Vector2> points = new List<Vector2>(_cubeVertices.Length);
                foreach (var v in _cubeVertices) {
                    var p0 = Vector3.Transform(v * Size, RotationMatrix);
                    points.Add(new Vector2(p0.X, p0.Z));
                }
                outline = Outline2D(points);
            }
            else // Cylinder and ring
            {
                //Flatten a mesh + create convex hull
                List<Vector2> points = new List<Vector2>(_cylinderVertices.Length);
                var sz = new Vector3(Size.X, Size.Y, Size.X);
                foreach (var v in _cylinderVertices) {
                    var p0 = Vector3.Transform(v * sz, RotationMatrix);
                    points.Add(new Vector2(p0.X, p0.Z));
                }
                outline = Outline2D(points);
            }
            return outline;
        }

        static Vector2[] Outline2D(List<Vector2> points)
        {
            if (points == null || points.Count <= 1)
                throw new InvalidOperationException();
            int n = points.Count, k = 0;
            var H = new Vector2[2 * n];
            points.Sort((a, b) =>
                MathF.Abs(a.X - b.X) < float.Epsilon ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));
            // Build lower hull
            for (int i = 0; i < n; ++i)
            {
                while (k >= 2 && Cross2D(H[k - 2], H[k - 1], points[i]) <= 0)
                    k--;
                H[k++] = points[i];
            }
            // Build upper hull
            for (int i = n - 2, t = k + 1; i >= 0; i--)
            {
                while (k >= t && Cross2D(H[k - 2], H[k - 1], points[i]) <= 0)
                    k--;
                H[k++] = points[i];
            }
            // Don't triangulate
            return H.Take(k).ToArray();
        }

        public Vector2[] TopDownMesh()
        {
            if(topDown != null)
                return topDown;

            if (Shape == ShapeKind.Ellipsoid)
            {
                topDown = new Vector2[(SECTORS - 1) * 3];
                int j = 0;
                for (int i = 1; i < SECTORS; i++)
                {
                    var lastT = ((2 * MathF.PI) / (SECTORS - 1)) * (i - 1);
                    var t = ((2 * MathF.PI) / (SECTORS - 1)) * i;
                    var p0 = Vector3.Transform(PrimitiveMath.GetPointOnRadius(Size, 0, lastT), RotationMatrix);
                    var p1 = Vector3.Transform(PrimitiveMath.GetPointOnRadius(Size, 0, t), RotationMatrix);
                    topDown[j++] = new Vector2(p0.X, p0.Z);
                    j++; //Vector2.Zero
                    topDown[j++] = new Vector2(p1.X, p1.Z);
                }
            }
            else if (Shape == ShapeKind.Sphere)
            {
                // Don't rotate for sphere
                topDown = new Vector2[(SECTORS - 1) * 3];
                int j = 0;
                var sz = new Vector3(Size.X);
                for (int i = 1; i < SECTORS; i++)
                {
                    var lastT = ((2 * MathF.PI) / (SECTORS - 1)) * (i - 1);
                    var t = ((2 * MathF.PI) / (SECTORS - 1)) * i;
                    var p0 = PrimitiveMath.GetPointOnRadius(sz, 0, lastT);
                    var p1 = PrimitiveMath.GetPointOnRadius(sz, 0, t);
                    topDown[j++] = new Vector2(p0.X, p0.Z);
                    j++; //Vector2.Zero
                    topDown[j++] = new Vector2(p1.X, p1.Z);
                }
            }
            else if (Shape == ShapeKind.Box)
            {
                //Flatten a mesh + create convex hull
                List<Vector2> points = new List<Vector2>(_cubeVertices.Length);
                foreach (var v in _cubeVertices) {
                    var p0 = Vector3.Transform(v * Size, RotationMatrix);
                    points.Add(new Vector2(p0.X, p0.Z));
                }
                topDown = Convex2D(points);
            }
            else // Cylinder and ring
            {
                //Flatten a mesh + create convex hull
                List<Vector2> points = new List<Vector2>(_cylinderVertices.Length);
                var sz = new Vector3(Size.X, Size.Y, Size.X);
                foreach (var v in _cylinderVertices) {
                    var p0 = Vector3.Transform(v * sz, RotationMatrix);
                    points.Add(new Vector2(p0.X, p0.Z));
                }
                topDown = Convex2D(points);
            }

            return topDown;
        }

        static double Cross2D(Vector2 O, Vector2 A, Vector2 B) =>
            ((double)A.X - O.X) * ((double)B.Y - O.Y) - ((double)A.Y - O.Y) * ((double)B.X - O.X);

        static Vector2[] Convex2D(List<Vector2> points)
        {
            if (points == null || points.Count <= 1)
                throw new InvalidOperationException();
            int n = points.Count, k = 0;
            var H = new Vector2[2 * n];
            points.Sort((a, b) =>
                MathF.Abs(a.X - b.X) < float.Epsilon ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));
            // Build lower hull
            for (int i = 0; i < n; ++i)
            {
                while (k >= 2 && Cross2D(H[k - 2], H[k - 1], points[i]) <= 0)
                    k--;
                H[k++] = points[i];
            }
            // Build upper hull
            for (int i = n - 2, t = k + 1; i >= 0; i--)
            {
                while (k >= t && Cross2D(H[k - 2], H[k - 1], points[i]) <= 0)
                    k--;
                H[k++] = points[i];
            }

            // Triangulate and return
            var triangulated = new Vector2[(k - 3) * 3];
            int j = 0;
            for (int i = 1; i < k - 2; i++)
            {
                triangulated[j++] = H[0];
                triangulated[j++] = H[i];
                triangulated[j++] = H[i + 1];
            }
            return triangulated;
        }

        public ref Vector3 RotationAngles => ref data.RotationAngles;
        public ref float EdgeFraction => ref data.EdgeFraction;
        public ref ZonePropFlags PropertyFlags => ref data.PropertyFlags;
        public ref Color4? PropertyFogColor => ref data.PropertyFogColor;
        public ref float Sort => ref data.Sort;
        public ref VisitFlags VisitFlags => ref data.VisitFlags;

        //Properties not yet used in game, but copied from ini for round trip support
        public string[] PopType;
        public string Music;
        public string Spacedust;
        public ref int SpacedustMaxParticles => ref data.SpacedustMaxParticles;
        public ref float Interference => ref data.Interference;
        public ref float PowerModifier => ref data.PowerModifier;
        public ref float DragModifier => ref data.DragModifier;
        public string Comment;
        public ref int LaneId => ref data.LaneId;
        public ref int TradelaneAttack => ref data.TradelaneAttack;
        public ref int TradelaneDown => ref data.TradelaneDown;
        public ref float Damage => ref data.Damage;
        public ref int Toughness => ref data.Toughness;
        public ref int Density => ref data.Density;
        public ref bool? PopulationAdditive => ref data.PopulationAdditive;
        public ref bool MissionEligible => ref data.MissionEligible;
        public ref int MaxBattleSize => ref data.MaxBattleSize;
        public ref int ReliefTime => ref data.ReliefTime;
        public ref int RepopTime => ref data.RepopTime;

        //Encounter parameters
        public string[] AttackIds;
        public string[] MissionType;
        public string[] PathLabel;
        public string[] Usage;
        public string VignetteType;
        public Encounter[] Encounters;
        public DensityRestriction[] DensityRestrictions;

		public Zone ()
		{
		}

        public bool ZonesEqual(Zone other)
        {
            return other.Nickname == Nickname &&
                   other.Comment == Comment &&
                   other.VignetteType == VignetteType &&
                   other.Music == Music &&
                   other.Spacedust == Spacedust &&
                   StructsEqual(ref data, ref other.data) &&
                   ArraysEqual(PopType, other.PopType) &&
                   ArraysEqual(AttackIds, other.AttackIds) &&
                   ArraysEqual(MissionType, other.MissionType) &&
                   ArraysEqual(PathLabel, other.PathLabel) &&
                   ArraysEqual(Usage, other.Usage) &&
                   ArraysEqual(Encounters, other.Encounters) &&
                   ArraysEqual(DensityRestrictions, other.DensityRestrictions);
        }

        public void CopyTo(Zone other)
        {
            other.Nickname = Nickname;
            other.data = data;
            other._shape = _shape;
            other.PopType = PopType.ShallowCopy();
            other.Music = Music;
            other.Spacedust = Spacedust;
            other.Comment = Comment;
            other.AttackIds = AttackIds.ShallowCopy();
            other.MissionType = MissionType.ShallowCopy();
            other.PathLabel = PathLabel.ShallowCopy();
            other.Usage = Usage.ShallowCopy();
            other.VignetteType = VignetteType;
            other.Encounters = Encounters.CloneCopy();
            other.DensityRestrictions = DensityRestrictions.ShallowCopy();
        }

        public Zone Clone()
        {
            var z = new Zone();
            CopyTo(z);
            return z;
        }
    }

    [Flags]
    public enum ZonePropFlags
    {
        None = 0,
        ObjDensityLow = 1,
        ObjDensityMed = 2,
        ObjDensityHigh = 4,
        DangerLow = 8,
        DangerMed = 16,
        DangerHigh = 32,
        Rock = 64,
        Debris = 128,
        Ice = 256,
        Lava = 512,
        Nomad = 1024,
        Crystal = 2048,
        Mines = 4096,
        Badlands = 8192,
        GasPockets = 16384,
        Cloud = 32768,
        Exclusion1 = 65536,
        Exclusion2 = 131072,
        Damage = 0x040000,
        DragModifier = 0x080000,
        Interference  = 0x100000,
        Spacedust = 0x200000,
        Music = 0x200000
    }
}

