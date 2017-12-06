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
		const float PRECISION = 32767f;

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
				maxIndex = 2;
				sign = q.W < 0 ? -1 : 1;
			}

			if (Math.Abs(1f - maxValue) < 0.0001f) //single element is one
			{
				om.Write((byte)(maxIndex + 4));
				return;
			}

			short a, b, c;
			if (maxIndex == 0)
			{
				a = (short)(q.Y * sign * PRECISION);
				b = (short)(q.Z * sign * PRECISION);
				c = (short)(q.W * sign * PRECISION);
			}
			else if (maxIndex == 1)
			{
				a = (short)(q.X * sign * PRECISION);
				b = (short)(q.Z * sign * PRECISION);
				c = (short)(q.W * sign * PRECISION);
			}
			else if (maxIndex == 2)
			{
				a = (short)(q.X * sign * PRECISION);
				b = (short)(q.Y * sign * PRECISION);
				c = (short)(q.W * sign * PRECISION);
			}
			else
			{
				a = (short)(q.X * sign * PRECISION);
				b = (short)(q.Y * sign * PRECISION);
				c = (short)(q.Z * sign * PRECISION);
			}

			om.Write((byte)maxIndex);
			om.Write(a);
			om.Write(b);
			om.Write(c);
		}

		public static Quaternion ReadQuaternion(Lidgren.Network.NetIncomingMessage im)
		{
			var maxIndex = im.ReadByte();

			if (maxIndex >= 4 && maxIndex <= 7)
			{
				var x = (maxIndex == 4) ? 1f : 0f;
				var y = (maxIndex == 5) ? 1f : 0f;
				var z = (maxIndex == 6) ? 1f : 0f;
				var w = (maxIndex == 7) ? 1f : 0f;
				return new Quaternion(x, y, z, w);
			}

			var a = (float)im.ReadInt16() / PRECISION;
			var b = (float)im.ReadInt16() / PRECISION;
			var c = (float)im.ReadInt16() / PRECISION;
			var d = (float)Math.Sqrt(1f - (a * a + b * b + c * c));

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
