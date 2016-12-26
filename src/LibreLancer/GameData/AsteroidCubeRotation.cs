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
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
using System;

namespace LibreLancer.GameData
{
	public class AsteroidCubeRotation
	{
		public static readonly Vector4 Default_AxisX = new Vector4(0, 90, 180, 270);
		public static readonly Vector4 Default_AxisY = new Vector4(0, 90, 180, 270);
		public static readonly Vector4 Default_AxisZ = new Vector4(0, 90, 180, 270);

		public Vector4 AxisX
		{
			get 
			{
				return axisx;
			} 
			set 
			{
				axisx = value;
				_x = new Vector4(
					MathHelper.DegreesToRadians(axisx.X),
					MathHelper.DegreesToRadians(axisx.Y),
					MathHelper.DegreesToRadians(axisx.Z),
					MathHelper.DegreesToRadians(axisx.W)
				);
			}
		}
		public Vector4 AxisY
		{
			get
			{
				return axisy;
			}
			set
			{
				axisy = value;
				_y = new Vector4(
					MathHelper.DegreesToRadians(axisy.X),
					MathHelper.DegreesToRadians(axisy.Y),
					MathHelper.DegreesToRadians(axisy.Z),
					MathHelper.DegreesToRadians(axisy.W)
				);
			}
		}
		public Vector4 AxisZ
		{
			get
			{
				return axisz;
			}
			set
			{
				axisz = value;
				_z = new Vector4(
					MathHelper.DegreesToRadians(axisz.X),
					MathHelper.DegreesToRadians(axisz.Y),
					MathHelper.DegreesToRadians(axisz.Z),
					MathHelper.DegreesToRadians(axisz.W)
				);
			}
		}

		Vector4 axisx;
		Vector4 axisy;
		Vector4 axisz;

		Vector4 _x;
		Vector4 _y;
		Vector4 _z;

		public Vector3 GetRotation(float param)
		{
			
			if (param < 0.25f)
				return new Vector3(_x.X, _y.X, _z.X);
			else if (param < 0.5f)
				return new Vector3(_x.Y, _y.Y, _z.Y);
			else if (param < 0.75f)
				return new Vector3(_x.Z, _y.Z, _z.Z);
			else
				return new Vector3(_x.W, _y.W, _z.W);
		}
	}
}
