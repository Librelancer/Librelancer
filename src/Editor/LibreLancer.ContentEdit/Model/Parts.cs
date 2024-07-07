// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Text;
using System.IO;
using LibreLancer;
using LibreLancer.Utf;
namespace LibreLancer.ContentEdit.Model
{
    public class SphereConstructor : PartNodeConstructor
    {
        public void Add(SphereConstruct con)
        {
            StartAdd(con.ParentName, con.ChildName, con.Origin);
            writer.Write(con.Offset.X);
            writer.Write(con.Offset.Y);
            writer.Write(con.Offset.Z);
            WriteMatrix3x3(con.Rotation);
            writer.Write(con.Min1);
            writer.Write(con.Max1);
            writer.Write(con.Min2);
            writer.Write(con.Max2);
            writer.Write(con.Min3);
            writer.Write(con.Max3);
        }
    }
    public class PrisConstructor : PartNodeConstructor
    {
        public void Add(PrisConstruct con)
        {
            StartAdd(con.ParentName, con.ChildName, con.Origin);
            writer.Write(con.Offset.X);
            writer.Write(con.Offset.Y);
            writer.Write(con.Offset.Z);
            WriteMatrix3x3(con.Rotation);
            writer.Write(con.AxisTranslation.X);
            writer.Write(con.AxisTranslation.Y);
            writer.Write(con.AxisTranslation.Z);
            writer.Write(con.Min);
            writer.Write(con.Max);
        }
    }
    public class RevConstructor : PartNodeConstructor
    {
        public void Add(RevConstruct con)
        {
            StartAdd(con.ParentName, con.ChildName, con.Origin);
            writer.Write(con.Offset.X);
            writer.Write(con.Offset.Y);
            writer.Write(con.Offset.Z);
            WriteMatrix3x3(con.Rotation);
            writer.Write(con.AxisRotation.X);
            writer.Write(con.AxisRotation.Y);
            writer.Write(con.AxisRotation.Z);
            writer.Write(con.Min);
            writer.Write(con.Max);
        }
    }
    public class FixConstructor : PartNodeConstructor
    {
        public void Add(FixConstruct con)
        {
            StartAdd(con.ParentName, con.ChildName, con.Origin);
            WriteMatrix3x3(con.Rotation);
        }
    }

    public abstract class PartNodeConstructor
    {
        MemoryStream stream = new MemoryStream();
        protected BinaryWriter writer;
        internal PartNodeConstructor()
        {
            writer = new BinaryWriter(stream);
        }
        protected void StartAdd(string parentName, string objectName, Vector3 origin)
        {
            var pbytes = Encoding.ASCII.GetBytes(parentName);
            writer.Write(pbytes);
            for (int i = 0; i < (64 - pbytes.Length); i++)
            {
                writer.Write((byte)0);
            }
            var cbytes = Encoding.ASCII.GetBytes(objectName);
            writer.Write(cbytes);
            for (int i = 0; i < (64 - cbytes.Length); i++)
            {
                writer.Write((byte)0);
            }
            writer.Write(origin.X);
            writer.Write(origin.Y);
            writer.Write(origin.Z);
        }
        protected void WriteMatrix3x3(Quaternion q)
        {
            var mat = Matrix4x4.CreateFromQuaternion(q);
            writer.Write(mat.M11);
            writer.Write(mat.M21);
            writer.Write(mat.M31);
            writer.Write(mat.M12);
            writer.Write(mat.M22);
            writer.Write(mat.M32);
            writer.Write(mat.M13);
            writer.Write(mat.M23);
            writer.Write(mat.M33);
        }
        public byte[] GetData()
        {
            return stream.ToArray();
        }
    }
}
