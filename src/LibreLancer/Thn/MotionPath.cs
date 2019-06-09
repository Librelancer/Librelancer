//Code adapted from: https://github.com/Tagussan/BSpline
//This file is licensed under the MIT license
using System;
using System.Collections.Generic;
using LibreLancer.Thorn;
namespace LibreLancer
{
	/// <summary>
	/// Represents a series of points as a b-spline
	/// </summary>
	public class MotionPath
	{
		const int OPEN = 1;
		const int CLOSED = 0;
		static Dictionary<string, object> env = new Dictionary<string, object>()
		{
			{ "OPEN", OPEN },
			{ "CLOSED", CLOSED }
		};

		bool loop;
		public bool Closed
		{
			get
			{
				return loop;
			}
		}

		public bool HasOrientation
		{
			get
			{
				return quaternions.Count > 0;
			}
		}

		Vector3 GetVec(object o)
		{
			var tab = (LuaTable)o;
			return new Vector3((float)tab[0], (float)tab[1], (float)tab[2]);
		}

		Quaternion GetQuat(object o)
		{
			var tab = (LuaTable)o;
			return new Quaternion((float)tab[0], (float)tab[1], (float)tab[2], (float)tab[3]);
		}

		List<Vector3> points = new List<Vector3>();
        Vector3[] tangents;
		List<Quaternion> quaternions = new List<Quaternion>();

        Matrix4 coefficients;
		public MotionPath(string pathdescriptor)
		{
			//Abuse the Lua runtime to parse the path descriptor for us
			var rt = new LuaRunner(env);
			var path = (LuaTable)(rt.DoString("path = {" + pathdescriptor + "}")["path"]);
			bool hasOrientation = true;
			var type = (int)path[0];
			loop = type == CLOSED;
			//detect if orientations are present
			var orient = (LuaTable)path[2];
			if (orient.Capacity < 4) {
				hasOrientation = false;
			}
			//Construct path
			for (int i = 1; i < path.Capacity; i++) {
				if (hasOrientation && i % 2 == 0)
					quaternions.Add(GetQuat(path[i]));
				else
					points.Add(GetVec(path[i]));
			}
            if(loop) {
                points.Add(points[0]);
                quaternions.Add(quaternions[0]);
            }


            if (points.Count > 2)
				curve = true;
            //
            if (curve)
            {
                coefficients = new Matrix4(
                    2, -2, 1, 1,
                    -3, 3, -2, -1,
                    0, 0, 1, 0,
                    1, 0, 0, 0
                );
                CalculateTangents();
            }
		}
		bool curve = false;

        void CalculateTangents()
        {
            tangents = new Vector3[points.Count];
            for(int i = 0; i < points.Count; i++)
            {
                if (i == 0)
                {
                    if (loop)
                        tangents[i] = 0.5f * (points[1] - points[points.Count - 2]);
                    else
                        tangents[i] = 0.5f * (points[1] - points[points.Count - 1]);
                }
                else if (i == points.Count - 1)
                {
                    if (loop)
                        tangents[i] = tangents[0];
                    else
                        tangents[i] = 0.5f * (points[i] - points[i - 1]);
                }
                else
                    tangents[i] = 0.5f * (points[i + 1] - points[i - 1]);
            }
        }

        Vector3 interpolate(float t)
        {
            float seg = t * (points.Count - 1);
            int segIdx = (int)seg;
            t = seg - segIdx;
            return interpolate(segIdx, t);
        }
        Vector3 interpolate(int index, float t)
        {
            if ((index + 1) >= points.Count) return points[points.Count - 1];

            float t2, t3;
            t2 = t * t;
            t3 = t2 * t;
            var powers = new Vector4(t3, t2, t, 1);
            var point1 = points[index];
            var point2 = points[index + 1];
            var tan1 = tangents[index];
            var tan2 = tangents[index + 1];

            var pt = new Matrix4(
                point1.X,point1.Y,point1.Z,1,
                point2.X,point2.Y,point2.Z,1,
                tan1.X,tan1.Y,tan1.Z,1,
                tan2.Z,tan2.Y,tan2.Z,1
            );

            var ret = powers * coefficients * pt;
            return ret.Xyz;
        }
        public Vector3 GetPosition(float t)
		{
            if (t >= 1) return points[points.Count - 1];
            if (t <= 0) return points[0];
			if (curve)
			{
                return interpolate(t);
			}
			else
			{
				float dist = VectorMath.Distance(points[0], points[1]);
				var direction = (points[1] - points[0]).Normalized();
				return points[0] + (direction * (dist * t));
			}
		}

		public Vector3 GetDirection(float t, bool reverse = false)
		{
			t = MathHelper.Clamp(t, 0, 1);
			if (curve)
			{
				Vector3 start = Vector3.Zero;
				Vector3 end = Vector3.Zero;
				if (t == 1)
				{
					end = GetPosition(1);
					while (true)
					{
						t -= 0.001f;
						start = GetPosition(t);
						if ((end - start).Length > 0.001)
							break;
					}
				}
				else
				{
					start = GetPosition(t);
					int j = 0;
					while (true)
					{
						j++;
						t += 0.001f;
						end = GetPosition(t);
						if ((end - start).Length > 0.001)
							break;
						if (j > 3)
						{
							end = GetPosition(1);
							break;
						}
					}
				}
				if (reverse)
					return (end - start).Normalized();
				else
					return (start - end).Normalized();
			}
			else
			{
				if (reverse)
					return (points[1] - points[0]).Normalized();
				else
					return (points[0] - points[1]).Normalized();
			}
		}
		public Quaternion GetOrientation(float t)
		{
			if (!HasOrientation)
				throw new NotSupportedException();
			t = MathHelper.Clamp(t, 0, 1);
			if (curve)
			{
				throw new NotSupportedException();
			}
			else
			{
				return Quaternion.Slerp(quaternions[0], quaternions[1], t);
			}
		}
	}
}
