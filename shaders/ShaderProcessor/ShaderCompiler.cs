using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

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


    /* EXAMPLE:
     * layout(std140) uniform Camera_Matrices
       {
           mat4 View;
           mat4 Projection;
           mat4 ViewProjection;
           vec3 CameraPosition;
       } _377;
       ^ we need to remove this identifier, and then prepend
         a stable identifier to all fields

         e.g. output:

       layout(std140) uniform Camera_Matrices
       {
           mat4 _abc_View;
           mat4 _abc_Projection;
           mat4 _abc_ViewProjection;
           vec3 _abc_CameraPosition;
       };

       These must match between vertex and fragment shaders for the same buffer name.
       The above "_377" identifier is only valid in GLSL 150 and not GLSL 140 which we
       target. The source must then be transformed to turn _377.View into _abc_View
     */

    record struct UBO(string Name, string Identifier, string Variables, int Start, int Length);

    static UBO? GetUBO(string source)
    {
        const string UBOIDENT = "layout(std140) uniform";
        int identEnd = 0;
        string identifier = "";
        while (string.IsNullOrWhiteSpace(identifier))
        {
            int sIdx = source.IndexOf(UBOIDENT, identEnd, StringComparison.Ordinal);
            if (sIdx == -1)
                return null;
            var blockStart = source.IndexOf("{", sIdx, StringComparison.Ordinal);
            string blockName = source.Substring(sIdx + UBOIDENT.Length, blockStart - sIdx - UBOIDENT.Length).Trim();
            int braces = 1;
            int endIdx = blockStart + 1;
            for (; endIdx < source.Length; endIdx++)
            {
                if (source[endIdx] == '{')
                {
                    throw new Exception("Unexpected {");
                }
                else if (source[endIdx] == '}')
                {
                    braces--;
                }
                if (braces == 0)
                    break;
            }

            var variables = source.Substring(blockStart + 1, endIdx - blockStart - 1);
            var identStart = endIdx + 1;
            identEnd = source.IndexOf(';', identStart);
            identifier = source.Substring(identStart, identEnd - identStart).Trim();
            if (!string.IsNullOrWhiteSpace(identifier))
            {
                return new(blockName, identifier, variables, sIdx, (identEnd + 1) - sIdx);
            }
        }
        return null;
    }


    private const string ALPHABET = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_";
    static string Encode(long number)
    {
        if (number < 0) {
            throw new ArgumentException("number < 0");
        }
        var builder = new StringBuilder();
        var divisor = ALPHABET.Length;
        while (number > 0)
        {
            number = Math.DivRem(number, divisor, out var rem);
            builder.Append(ALPHABET[(int) rem]);
        }
        return builder.ToString();
    }

    static bool FixUBOs(ref string source)
    {
        var ubo = GetUBO(source);
        if (ubo == null)
            return false;
        var fieldRegex = new Regex(@"\s([0-9A-Za-z_\[\]]*);");
        List<string> variableNames = new();
        foreach (Match m in fieldRegex.Matches(ubo.Value.Variables))
        {
            variableNames.Add(m.Groups[1].Value.Split('[')[0]);
        }

        // hash truncated to 24-bit, encoded with slightly more characters than hex.
        var newIdentifier = $"UB_{Encode(((uint)ubo.Value.Name.GetHashCode()) & 0xFFFFFF)}";
        var newVariables = fieldRegex.Replace(ubo.Value.Variables, $" {newIdentifier}_$1;");
        var newBlock = $@"layout (std140) uniform {ubo.Value.Name}
{{{newVariables}}};";
        source = source.Substring(0, ubo.Value.Start) +
                 newBlock + source.Substring(ubo.Value.Start + ubo.Value.Length);
        foreach (var v in variableNames)
        {
            source = source.Replace($"{ubo.Value.Identifier}.{v}", $"{newIdentifier}_{v}");
        }
        return true;
    }

    public static string SHCompile(string source, string filename, string defs, ShaderKind kind)
    {
        string defines = kind switch
        {
            ShaderKind.Vertex => "#define VERTEX_SHADER\n" + defs,
            ShaderKind.Fragment => "#define FRAGMENT_SHADER\n" + defs,
            _ => throw new InvalidOperationException(),
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
            while (FixUBOs(ref ret)) ;
            SHFreeResult(result);
            return ret;
        }

        throw new Exception("Compilation failed");
    }
}
