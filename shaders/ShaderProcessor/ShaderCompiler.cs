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
    private static extern IntPtr SHCompile(IntPtr source, IntPtr filename, IntPtr defines, bool vertex);

    [DllImport("shadercompiler")]
    public static extern void SHFinish();

    public static string SHCompile(string source, string filename, string defines, bool vertex)
    {
        if (vertex) defines = "#define VERTEX_SHADER\n" + defines;
        else defines = "#define FRAGMENT_SHADER\n" + defines;
        
        var src = Marshal.StringToCoTaskMemUTF8(source);
        var fn = Marshal.StringToCoTaskMemUTF8(filename);
        var def = Marshal.StringToCoTaskMemUTF8(defines);
        var result = SHCompile(src, fn, def, vertex);
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