using System;
using System.Numerics;

namespace LibreLancer.Graphics.Backends.Null;

class NullShader : IShader
{
    public int GetLocation(string name)
    {
        return -1;
    }

    public unsafe void SetMatrix(int loc, ref Matrix4x4 mat)
    {
    }

    public void SetMatrix(int loc, IntPtr mat)
    {
    }

    public void SetInteger(int loc, int value, int index = 0)
    {
    }

    public void SetFloat(int loc, float value, int index = 0)
    {
    }

    public void SetColor4(int loc, Color4 value, int index = 0)
    {
    }

    public void SetVector4(int loc, Vector4 value, int index = 0)
    {
    }

    public unsafe void SetVector4Array(int loc, Vector4* values, int count)
    {
    }

    public unsafe void SetVector3Array(int loc, Vector3* values, int count)
    {
    }

    public void SetVector4i(int loc, Vector4i value, int index = 0)
    {
    }

    public void SetVector3(int loc, Vector3 vector, int index = 0)
    {
    }

    public void SetVector2(int loc, Vector2 vector, int index = 0)
    {
    }

    public void UniformBlockBinding(string uniformBlock, int index)
    {
    }

    public void UseProgram()
    {
    }
}
