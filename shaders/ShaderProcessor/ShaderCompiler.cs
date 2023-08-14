using System;
using System.Runtime.InteropServices;

namespace ShaderProcessor;

public class ShaderCompiler
{
    [DllImport("shadercompiler")]
    public static extern void SHInit();

    [DllImport("shadercompiler")]
    private static extern void SHFreeResult(IntPtr result);

    [DllImport("shadercompiler")]
    private static extern IntPtr SHCompile(IntPtr source, IntPtr filename, IntPtr defines, int kind);

    [DllImport("shadercompiler")]
    public static extern void SHFinish();

    public enum ShaderKind : int
    {
        Vertex = 0,
        Fragment = 1,
        Geometry = 2,
    }

    public static string SHCompile(string source, string filename, string defs, ShaderKind kind)
    {
        string defines = kind switch
        {
            ShaderKind.Vertex => "#define VERTEX_SHADER\n" + defs,
            ShaderKind.Fragment => "#define FRAGMENT_SHADER\n" + defs,
            ShaderKind.Geometry => "#define GEOMETRY_SHADER\n" + defs,
        };
        
        var src = Marshal.StringToCoTaskMemUTF8(source);
        var fn = Marshal.StringToCoTaskMemUTF8(filename);
        var def = Marshal.StringToCoTaskMemUTF8(defines);
        var result = SHCompile(src, fn, def, (int)kind);
        Marshal.ZeroFreeCoTaskMemUTF8(src);
        Marshal.ZeroFreeCoTaskMemUTF8(fn);
        Marshal.ZeroFreeCoTaskMemUTF8(def);
        if (result != IntPtr.Zero)
        {
            var ret = Marshal.PtrToStringUTF8(result);
            SHFreeResult(result);
            return ret;
        }

        throw new Exception("Compilation failed");
    }
}