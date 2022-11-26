// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using System.Runtime.InteropServices;

namespace LibreLancer.Render
{
	//PointLight struct used by shaders (Features430 only)
	[StructLayout(LayoutKind.Sequential)]
	public struct PointLight
	{
		public Vector4 Position;
		public Vector4 ColorRange;
		public Vector4 Attenuation;

		Vector4 padding;
	}
}
