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
    public static class VectorMath
    {
        public static float Distance(Vector3 a, Vector3 b)
        {
            float result;
            result = (a.X - b.X) * (a.X - b.X) +
                (a.Y - b.Y) * (a.Y - b.Y) +
                    (a.Z - b.Z) * (a.Z - b.Z);
            return (float)Math.Sqrt(result);
        }

		public static float Distance2D(Vector2 a, Vector2 b)
		{
			return (float)Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
		}

		public static float DistanceSquared(Vector3 a, Vector3 b)
		{
			return (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y) + (a.Z - b.Z) * (a.Z - b.Z);
		}

		public static Vector3 Transform(Vector3 position, Matrix4 matrix)
		{
			var result = Vector4.Transform (new Vector4 (position,1 ),matrix);
			return result.Xyz;
		}
    }
}