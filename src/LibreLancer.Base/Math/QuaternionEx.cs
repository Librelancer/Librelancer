// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
namespace LibreLancer
{
    public class QuaternionEx
    {
        public static Quaternion LookAt(Vector3 sourcePoint, Vector3 destPoint)
        {
            var forwardVector = Vector3.Normalize(destPoint - sourcePoint);

            float dot = Vector3.Dot(-Vector3.UnitZ, forwardVector);

            if (Math.Abs(dot - (-1.0f)) < 0.000001f)
            {
                //TODO: This is broken
                return new Quaternion(0,1,0, 0);
            }
            if (Math.Abs(dot - (1.0f)) < 0.000001f)
            {
                return new Quaternion(0, 0, 0, 1);
            }

            float rotAngle = (float)Math.Acos(dot);
            Vector3 rotAxis = Vector3.Cross(-Vector3.UnitZ, forwardVector);
            rotAxis = Vector3.Normalize(rotAxis);
            return Quaternion.CreateFromAxisAngle(rotAxis, rotAngle);
        }
        //based on https://stackoverflow.com/questions/52413464/look-at-quaternion-using-up-vector/52551983#52551983
        public static Quaternion LookRotation(Vector3 forward, Vector3 up)
        {
            var F = Vector3.Normalize(forward);
            var R = Vector3.Normalize(Vector3.Cross(up, forward));
            var U = Vector3.Cross(F, R);

            var m00 = R.X;
            var m01 = R.Y;
            var m02 = R.Z;
            var m10 = U.X;
            var m11 = U.Y;
            var m12 = U.Z;
            var m20 = F.X;
            var m21 = F.Y;
            var m22 = F.Z;
            var q = new Quaternion();


            float trace = m00 + m11 + m22;
            if (trace > 0f)
            {
                var s = 0.5f / (float)Math.Sqrt(trace + 1f);
                q.W = 0.25f / s;
                q.X = (m12 - m21) * s;
                q.Y = (m20 - m02) * s;
                q.Z = (m01 - m10) * s;
                return q;
            }
            else
            {
                if ((m00 >= m11) && (m00 >= m22))
                {
                    var s = 2f * (float)Math.Sqrt(1f + m00 - m11 - m22);
                    q.X = 0.25f * s;
                    q.Y = (m01 + m10) / s;
                    q.Z = (m02 + m20) / s;
                    q.W = (m12 - m21) / s;
                    return q;
                }
                else if (m11 > m22)
                {
                    var s = 2 * (float)Math.Sqrt(1f + m11 - m00 - m22);
                    q.X = (m10 + m01) / s;
                    q.Y = 0.25f * s;
                    q.Z = (m21 + m12) / s;
                    q.W = (m20 - m02) / s;
                    return q;
                }
                else
                {
                    var s = 2 * (float)Math.Sqrt(1f + m22 - m00 - m11);
                    q.X = (m20 + m02) / s;
                    q.Y = (m21 + m12) / s;
                    q.Z = 0.25f * s;
                    q.W = (m01 - m10) / s;
                    return q;
                }
            }
        }
    }
}