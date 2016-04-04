using System;
using OpenTK;
namespace LibreLancer
{
	public interface ICamera
	{
		Matrix4 ViewProjection { get; }
		Vector3 Position { get; }
	}
}

