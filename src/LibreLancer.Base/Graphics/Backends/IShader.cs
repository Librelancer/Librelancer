using System;
using System.Numerics;

namespace LibreLancer.Graphics.Backends;

interface IShader
{
    int GetLocation(string name);
    unsafe void SetMatrix(int loc, ref Matrix4x4 mat);
    void SetMatrix(int loc, IntPtr mat);
    void SetInteger(int loc, int value, int index = 0);
    void SetFloat(int loc, float value, int index = 0);
    void SetColor4(int loc, Color4 value, int index = 0);
    void SetVector4(int loc, Vector4 value, int index = 0);
    unsafe void SetVector4Array(int loc, Vector4* values, int count);
    unsafe void SetVector3Array(int loc, Vector3* values, int count);
    void SetVector4i(int loc, Vector4i value, int index = 0);
    void SetVector3(int loc, Vector3 vector, int index = 0);
    void SetVector2(int loc, Vector2 vector, int index = 0);
    void UniformBlockBinding(string uniformBlock, int index);
}
