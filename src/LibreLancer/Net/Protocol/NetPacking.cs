/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2017
 * the Initial Developer. All Rights Reserved.
 */
using System;
namespace LibreLancer
{
	public static class NetPacking
	{
        const int BITS_COMPONENT = 15;

        const float UNIT_MIN = -0.707107f;
        const float UNIT_MAX = 0.707107f;
		public static void WriteQuaternion(Lidgren.Network.NetOutgoingMessage om, Quaternion q)
		{
			var maxIndex = 0;
			var maxValue = float.MinValue;
			var sign = 1f;

			maxValue = Math.Abs(q.X);
			sign = q.X < 0 ? -1 : 1;

			if (Math.Abs(q.Y) > maxValue)
			{
				maxValue = Math.Abs(q.Y);
				maxIndex = 1;
				sign = q.Y < 0 ? -1 : 1;
			}
			if (Math.Abs(q.Z) > maxValue)
			{
				maxValue = Math.Abs(q.Z);
				maxIndex = 2;
				sign = q.Z < 0 ? -1 : 1;
			}
			if (Math.Abs(q.W) > maxValue)
			{
				maxValue = Math.Abs(q.W);
				maxIndex = 3;
				sign = q.W < 0 ? -1 : 1;
			}
            om.WriteRangedInteger(0, 3, maxIndex);

  			if (maxIndex == 0)
			{
                om.WriteRangedSingle(q.Y * sign, UNIT_MIN, UNIT_MAX, BITS_COMPONENT);
                om.WriteRangedSingle(q.Z * sign, UNIT_MIN, UNIT_MAX, BITS_COMPONENT);
                om.WriteRangedSingle(q.W * sign, UNIT_MIN, UNIT_MAX, BITS_COMPONENT);
			}
			else if (maxIndex == 1)
			{
                om.WriteRangedSingle(q.X * sign, UNIT_MIN, UNIT_MAX, BITS_COMPONENT);
                om.WriteRangedSingle(q.Z * sign, UNIT_MIN, UNIT_MAX, BITS_COMPONENT);
                om.WriteRangedSingle(q.W * sign, UNIT_MIN, UNIT_MAX, BITS_COMPONENT);
			}
			else if (maxIndex == 2)
			{
                om.WriteRangedSingle(q.X * sign, UNIT_MIN, UNIT_MAX, BITS_COMPONENT);
                om.WriteRangedSingle(q.Y * sign, UNIT_MIN, UNIT_MAX, BITS_COMPONENT);
                om.WriteRangedSingle(q.W * sign, UNIT_MIN, UNIT_MAX, BITS_COMPONENT);
			}
			else
			{
                om.WriteRangedSingle(q.X * sign, UNIT_MIN, UNIT_MAX, BITS_COMPONENT);
                om.WriteRangedSingle(q.Y * sign, UNIT_MIN, UNIT_MAX, BITS_COMPONENT);
                om.WriteRangedSingle(q.Z * sign, UNIT_MIN, UNIT_MAX, BITS_COMPONENT);
			}
            om.WritePadBits();
		}

		public static Quaternion ReadQuaternion(Lidgren.Network.NetIncomingMessage im)
		{
            var maxIndex = im.ReadRangedInteger(0, 3);

            var a = im.ReadRangedSingle(UNIT_MIN, UNIT_MAX, BITS_COMPONENT);
            var b = im.ReadRangedSingle(UNIT_MIN, UNIT_MAX, BITS_COMPONENT);
            var c = im.ReadRangedSingle(UNIT_MIN, UNIT_MAX, BITS_COMPONENT);
			var d = (float)Math.Sqrt(1f - (a * a + b * b + c * c));
            im.ReadPadBits();
			if (maxIndex == 0)
				return new Quaternion(d, a, b, c);
			if (maxIndex == 1)
				return new Quaternion(a, d, b, c);
			if (maxIndex == 2)
				return new Quaternion(a, b, d, c);
			return new Quaternion(a, b, c, d);
		}

		public static void WriteDirection(Lidgren.Network.NetOutgoingMessage om, Vector3 vec)
		{
			om.Write((short)(vec.X * short.MaxValue));
			om.Write((short)(vec.Y * short.MaxValue));
			om.Write((short)(vec.Z * short.MaxValue));
		}

		public static Vector3 ReadDirection(Lidgren.Network.NetIncomingMessage om)
		{
			return new Vector3(
				(float)om.ReadInt16() / short.MaxValue,
				(float)om.ReadInt16() / short.MaxValue,
				(float)om.ReadInt16() / short.MaxValue
			);
		}
	}
}
