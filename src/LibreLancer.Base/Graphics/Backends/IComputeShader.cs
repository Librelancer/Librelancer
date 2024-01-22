using System.Numerics;

namespace LibreLancer.Graphics.Backends;

public interface IComputeShader
{
    void Uniform1i(string name, int i);
    void Uniform2i(string name, Point pt);
    void UniformMatrix4fv(string name, ref Matrix4x4 mat);
    void Dispatch(uint groupsX, uint groupsY, uint groupsZ);
}
