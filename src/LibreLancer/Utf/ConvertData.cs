// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;


namespace LibreLancer
{
    public static unsafe class ConvertData
    {
        public static Vector2 ToVector2(BinaryReader reader)
        {
            Vector2 result = Vector2.Zero;

            result.X = reader.ReadSingle();
            result.Y = reader.ReadSingle();

            return result;
        }

        public static Vector3 ToVector3(byte[] data, int start = 0)
        {
            fixed (byte* pinned = data)
            {
                return *(Vector3*) (&pinned[start]);
            }
        }

        public static Vector3 ToVector3(BinaryReader reader)
        {
            return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        public static Vector3[] ToVector3Array(byte[] data, int start = 0, int length = 0)
        {
            var len = length == 0 ? data.Length : length;
            len /= (sizeof(float) * 3);
            if (len == 0) return new Vector3[0];
            Vector3[] result = new Vector3[len];
            for (int i = 0; i < len; i++)
            {
                result[i] = ToVector3(data, start + (i * sizeof(float) * 3));
            }
            return result;
        }

        public static Matrix4 ToMatrix3x3(byte[] data, int start = 0)
        {
            fixed (byte* pinned = data)
            {
                var floats = (float*) (&pinned[start]);
                var result = Matrix4.Identity;
                result.M11 = floats[0];
                result.M21 = floats[1];
                result.M31 = floats[2];
                result.M41 = 0;
                result.M12 = floats[3];
                result.M22 = floats[4];
                result.M32 = floats[5];
                result.M42 = 0;
                result.M13 = floats[6];
                result.M23 = floats[7];
                result.M33 = floats[8];
                result.M43 = 0;
                result.M14 = 0;
                result.M24 = 0;
                result.M34 = 0;
                result.M44 = 1;
                return result;
            }
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

        public static Matrix4 ToMatrix4x3(byte[] data, int start = 0)
        {
            var mat3 = ToMatrix3x3(data, start);
            mat3.Transpose();
            return Matrix4.CreateTranslation(-ToVector3(data, start + sizeof(float) * 9)) * mat3;
        }

        public static Color4 ToColor(byte[] data, int start = 0)
        {
            fixed (byte* pinned = data)
            {
                var floats = (float*) (&pinned[start]);
                return new Color4(floats[0], floats[1], floats[2], 1f);
            }
        }
    }
}