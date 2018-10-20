// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;


namespace LibreLancer
{
    public static class ConvertData
    {
        public static Vector2 ToVector2(byte[] data)
        {
            Vector2 result = Vector2.Zero;

            using (MemoryStream stream = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(stream);
                result = ToVector2(reader);
            }

            return result;
        }

        public static Vector2 ToVector2(BinaryReader reader)
        {
            Vector2 result = Vector2.Zero;

            result.X = reader.ReadSingle();
            result.Y = reader.ReadSingle();

            return result;
        }

        public static Vector2[] ToVector2Array(byte[] data)
        {
            Vector2[] result;

            using (MemoryStream stream = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(stream);
                result = ToVector2Array(reader);
            }

            return result;
        }

        public static Vector2[] ToVector2Array(BinaryReader reader)
        {
            List<Vector2> result = new List<Vector2>();

            while (reader.BaseStream.Position <= reader.BaseStream.Length - sizeof(float) * 2)
            {
                result.Add(new Vector2(reader.ReadSingle(), -reader.ReadSingle()));
            }

            return result.ToArray();
        }

        public static Vector3 ToVector3(byte[] data)
        {
            Vector3 result = Vector3.Zero;

            using (MemoryStream stream = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(stream);
                result = ToVector3(reader);
            }

            return result;
        }

        public static Vector3 ToVector3(BinaryReader reader)
        {
            Vector3 result = Vector3.Zero;

            result.X = reader.ReadSingle();
            result.Y = reader.ReadSingle();
            result.Z = reader.ReadSingle();

            return result;
        }

        public static Vector3[] ToVector3Array(byte[] data)
        {
            Vector3[] result;

            using (MemoryStream stream = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(stream);
                result = ToVector3Array(reader);
            }

            return result;
        }

        public static Vector3[] ToVector3Array(BinaryReader reader)
        {
            List<Vector3> result = new List<Vector3>();

            while (reader.BaseStream.Position <= reader.BaseStream.Length - sizeof(float) * 3)
            {
                result.Add(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
            }

            return result.ToArray();
        }

        public static Matrix4 ToMatrix3x3(byte[] data)
        {
            Matrix4 result = Matrix4.Identity;

            using (MemoryStream stream = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(stream);
                result = ToMatrix3x3(reader);
            }

            return result;
        }

        public static Matrix4 ToMatrix3x3(BinaryReader reader)
        {
            Matrix4 result = Matrix4.Identity;

            result.M11 = reader.ReadSingle();
            result.M21 = reader.ReadSingle();
            result.M31 = reader.ReadSingle();
            result.M41 = 0;
            result.M12 = reader.ReadSingle();
            result.M22 = reader.ReadSingle();
            result.M32 = reader.ReadSingle();
            result.M42 = 0;
            result.M13 = reader.ReadSingle();
            result.M23 = reader.ReadSingle();
            result.M33 = reader.ReadSingle();
            result.M43 = 0;
            result.M14 = 0;
            result.M24 = 0;
            result.M34 = 0;
            result.M44 = 1;

            return result;
        }

        public static Matrix4 ToMatrix4x3(byte[] data)
        {
            Matrix4 result = Matrix4.Identity;

            using (MemoryStream stream = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(stream);
                result = ToMatrix4x3(reader);
            }

            return result;
        }

        public static Matrix4 ToMatrix4x3(BinaryReader reader)
        {
            Matrix4 result = Matrix4.Identity;

            result.M11 = reader.ReadSingle();
            result.M21 = reader.ReadSingle();
            result.M31 = reader.ReadSingle();
            result.M41 = 0;
            result.M12 = reader.ReadSingle();
            result.M22 = reader.ReadSingle();
            result.M32 = reader.ReadSingle();
            result.M42 = 0;
            result.M13 = reader.ReadSingle();
            result.M23 = reader.ReadSingle();
            result.M33 = reader.ReadSingle();
            result.M43 = 0;
            result.M14 = reader.ReadSingle();
            result.M24 = reader.ReadSingle();
            result.M34 = reader.ReadSingle();
            result.M44 = 1;

            return result;
        }

        public static Color4 ToColor(byte[] data)
        {
            Color4 result = Color4.White;

            using (MemoryStream stream = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(stream);
                result = ToColor4(reader);
            }

            return result;
        }

        public static Color4 ToColor4(BinaryReader reader)
        {
            float r = reader.ReadSingle();
            //r = r < 0 ? 0 : r > 1 ? 1 : r;

            float g = reader.ReadSingle();
            //g = g < 0 ? 0 : g > 1 ? 1 : g;

            float b = reader.ReadSingle();
            //b = b < 0 ? 0 : b > 1 ? 1 : b;

            return new Color4(r, g, b, 1f);
        }
    }
}