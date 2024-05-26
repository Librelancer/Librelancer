using System.Numerics;

namespace LibreLancer.Render;

public interface IRenderHardpoint
{
    Matrix4x4 Transform { get; }
}
