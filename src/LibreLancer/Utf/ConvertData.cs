// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;


namespace LibreLancer
{
    //We don't use pointers to access data as they can cause data misalignment errors on Armhf
    public static class ConvertData
    {

        static float Float(byte[] data, int start, int idx) => BitConverter.ToSingle(data, start + idx * 4);


        public static Vector3 ToVector3(byte[] data, int start = 0)
        {
            return new Vector3(Float(data, start, 0), Float(data, start, 1), Float(data, start, 2));
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

        public static Matrix4x4 ToMatrix3x3(byte[] data, int start = 0)
        {
            var result = Matrix4x4.Identity;
                result.M11 = Float(data, start, 0);
                result.M21 = Float(data, start, 1);
                result.M31 = Float(data, start, 2);
                result.M41 = 0;
                result.M12 = Float(data, start, 3);
                result.M22 = Float(data, start, 4);
                result.M32 = Float(data, start, 5);
                result.M42 = 0;
                result.M13 = Float(data, start, 6);
                result.M23 = Float(data, start, 7);
                result.M33 = Float(data, start, 8);
                result.M43 = 0;
                result.M14 = 0;
            result.M24 = 0;
            result.M34 = 0;
            result.M44 = 1;
            return result;
        }

        public static Matrix4x4 ToMatrix3x3(BinaryReader reader)
        {
            Matrix4x4 result = Matrix4x4.Identity;

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

        public static Matrix4x4 ToMatrix4x3(byte[] data, int start = 0)
        {
            var mat3 = ToMatrix3x3(data, start);
            mat3 =  Matrix4x4.Transpose(mat3);
            return Matrix4x4.CreateTranslation(-ToVector3(data, start + sizeof(float) * 9)) * mat3;
        }

        public static Color4 ToColor(byte[] data, int start = 0)
        {
            return new Color4(Float(data, start, 0), Float(data, start, 1), Float(data, start, 2), 1f);
        }
    }
}