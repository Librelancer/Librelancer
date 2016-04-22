using System;
using OpenTK;
namespace LibreLancer
{
	public interface ICamera
	{
		Matrix4 ViewProjection { get; }
		Matrix4 Projection { get; }
		Matrix4 View { get; }
		Vector3 Position { get; }
		BoundingFrustum Frustum { get; }
	}
}

