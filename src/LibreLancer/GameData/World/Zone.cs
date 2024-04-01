// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using LibreLancer.Data.Universe;

namespace LibreLancer.GameData.World
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

        void UpdateShape()
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
        }

        public Vector3 PointOnRadius(Func<float> randfunc)
        {
            switch (data.Shape)
            {
                case ShapeKind.Sphere:
                {
                    var theta = randfunc () * 2 * Math.PI;
                    var phi = randfunc () * 2 * Math.PI;
                    var x = Math.Cos (theta) * Math.Cos (phi);
                    var y = Math.Sin (phi);
                    var z = Math.Sin (theta) * Math.Cos (phi);
                    return new Vector3 ((float)x, (float)y, (float)z) * data.Size.X;
                }
                case ShapeKind.Ellipsoid:
                {
                    var theta = randfunc () * 2 * Math.PI;
                    var phi = randfunc () * 2 * Math.PI;
                    var x = Math.Cos (theta) * Math.Cos (phi);
                    var y = Math.Sin (phi);
                    var z = Math.Sin (theta) * Math.Cos (phi);
                    return new Vector3 ((float)x, (float)y, (float)z) * data.Size;
                }
                default:
                    throw new InvalidOperationException();
            }
        }

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
                    throw new InvalidOperationException();
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
        public int[] IdsInfo = Array.Empty<int>();

        public Vector3 Position
        {
            get => data.Position;
            set
            {
                data.Position = value;
                UpdateShape();
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
                   ArraysEqual(IdsInfo, other.IdsInfo) &&
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
            other.IdsInfo = IdsInfo.ShallowCopy();
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

