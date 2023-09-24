// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;

namespace LibreLancer
{
	public interface ICamera
	{
		Matrix4x4 ViewProjection { get; }
		Matrix4x4 Projection { get; }
		Matrix4x4 View { get; }
		Vector3 Position { get; }

        bool FrustumCheck(BoundingSphere sphere);
        bool FrustumCheck(BoundingBox box);
	}
}

