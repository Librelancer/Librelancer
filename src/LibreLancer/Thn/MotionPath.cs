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
		List<Quaternion> quaternions = new List<Quaternion>();
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
			if (points.Count > 2)
				curve = true;
		}
		bool curve = false;


		const int DEGREE = 3;
		const int BASEFUNCRANGEINT = 2;

		float CubicBasis(float x)
		{
			if (-1 <= x && x < 0)
			{
				return 2.0f / 3.0f + (-1.0f - x / 2.0f) * x * x;
			}
			else if (1 <= x && x <= 2)
			{
				return 4.0f / 3.0f + x * (-2.0f + (1.0f - x / 6.0f) * x);
			}
			else if (-2 <= x && x < -1)
			{
				return 4.0f / 3.0f + x * (2.0f + (1.0f + x / 6.0f) * x);
			}
			else if (0 <= x && x < 1)
			{
				return 2.0f / 3.0f + (-1.0f + x / 2.0f) * x * x;
			}
			else
			{
				return 0;
			}
		}

		int seqAt<T>(int n, IList<T> list)
		{
			var margin = DEGREE + 1;
			if (n < margin)
				return 0;
			if (list.Count + margin <= n)
				return list.Count - 1;
			return n - margin;
		}

		float seqX(int n) => points[seqAt(n, points)].X;

		float seqY(int n) => points[seqAt(n, points)].Y;

		float seqZ(int n) => points[seqAt(n, points)].Z;

		float getInterpol(Func<int, float> seq, float t)
		{
			var tInt = (int)Math.Floor(t);
			float result = 0;
			for (int i = tInt - BASEFUNCRANGEINT; i <= tInt + BASEFUNCRANGEINT; i++) {
				result += seq(i) * CubicBasis(t - i);
			}
			return result;
		}

		public Vector3 GetPosition(float t)
		{
			t = MathHelper.Clamp(t, 0, 1);
			if (curve)
			{
				t = t * ((DEGREE + 1) * 2 + points.Count);
				return new Vector3(getInterpol(seqX, t), getInterpol(seqY, t), getInterpol(seqZ, t));
			}
			else
			{
				float dist = VectorMath.Distance(points[0], points[1]);
				var direction = (points[1] - points[0]).Normalized();
				return points[0] + (direction * (dist * t));
			}
		}

		public Vector3 GetDirection(float t)
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
						t -= 0.0001f;
						start = GetPosition(t);
						if ((end - start).Length > 0.001)
							break;
					}
				}
				else
				{
					start = GetPosition(t);
					while (true)
					{
						t += 0.0001f;
						start = GetPosition(t);
						if ((end - start).Length > 0.001)
							break;
					}
				}
				return (start - end).Normalized();
				//t = t * ((DEGREE + 1) * 2 + points.Count);
				//return new Vector3(getInterpol(seqX, t), getInterpol(seqY, t), getInterpol(seqZ, t));
			}
			else
			{
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
