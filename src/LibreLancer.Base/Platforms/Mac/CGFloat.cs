// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Platforms.Mac
{
	unsafe struct CGFloat
	{
		public static readonly CGFloat Zero = new CGFloat(0.0);
		IntPtr _backing;

		public CGFloat(float f)
		{
			_backing = IntPtr.Zero;
			SetFloat (f);
		}
		public CGFloat(double d)
		{
			_backing = IntPtr.Zero;
			SetDouble (d);
		}
		public CGFloat(IntPtr p)
		{
			_backing = p;
		}
		void SetFloat(float f)
		{
			if (IntPtr.Size == 8) {
				SetDouble ((double)f);
			} else {
				_backing = *(IntPtr*)&f;
			}
		}
		void SetDouble(double d)
		{
			if (IntPtr.Size == 4) {
				SetFloat ((float)d);
			} else {
				_backing = *(IntPtr*)&d;
			}
		}
		float GetFloat()
		{
			if (IntPtr.Size == 8) {
				return (float)GetDouble ();
			}
			fixed(IntPtr* bp = &_backing) {
				return *(float*)bp;
			}
		}
		double GetDouble()
		{
			if (IntPtr.Size == 4) {
				return (double)GetFloat ();
			}
			fixed(IntPtr* bp = &_backing) {
				return *(double*)bp;
			}
		}
		public override string ToString ()
		{
			if (IntPtr.Size == 4)
				return GetFloat ().ToString ();
			else
				return GetDouble ().ToString ();
		}
		public static implicit operator CGFloat(float f)
		{
			return new CGFloat (f);
		}
		public static explicit operator CGFloat(double d)
		{
			return new CGFloat (d);
		}
		public static explicit operator CGFloat(int i)
		{
			return new CGFloat((double)i);
		}
		public static implicit operator double(CGFloat cg)
		{
			return cg.GetDouble ();
		}
		public static explicit operator float(CGFloat cg)
		{
			return cg.GetFloat ();
		}
		public static implicit operator IntPtr(CGFloat ns)
		{
			return ns._backing;
		}
		public static implicit operator CGFloat(IntPtr ip)
		{
			return new CGFloat(ip);
		}
		public static CGFloat operator +(CGFloat a, CGFloat b)
		{
			if (IntPtr.Size == 4)
				return new CGFloat (a.GetFloat () + b.GetFloat ());
			else
				return new CGFloat (a.GetDouble () + b.GetDouble ());
		}
		public static CGFloat operator -(CGFloat a, CGFloat b)
		{
			if (IntPtr.Size == 4)
				return new CGFloat (a.GetFloat () - b.GetFloat ());
			else
				return new CGFloat (a.GetDouble () - b.GetDouble ());
		}
		public static CGFloat operator *(CGFloat a, CGFloat b)
		{
			if (IntPtr.Size == 4)
				return new CGFloat (a.GetFloat () * b.GetFloat ());
			else
				return new CGFloat (a.GetDouble () * b.GetDouble ());
		}
		public static CGFloat operator /(CGFloat a, CGFloat b)
		{
			if (IntPtr.Size == 4)
				return new CGFloat (a.GetFloat () / b.GetFloat ());
			else
				return new CGFloat (a.GetDouble () / b.GetDouble ());
		}
		public static CGFloat operator %(CGFloat a, CGFloat b)
		{
			if (IntPtr.Size == 4)
				return new CGFloat (a.GetFloat () % b.GetFloat ());
			else
				return new CGFloat (a.GetDouble () % b.GetDouble ());
		}

	}
}

