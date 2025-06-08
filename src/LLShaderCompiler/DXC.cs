using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace LLShaderCompiler;

// The compile times for DirectXShaderCompiler are -astronomical-
// We use the dxc binary instead of linking to the library so we can
// easily use prebuilt distributables.

static class DXC
{
    public static string OverridePath = "";

    static string? GetDXCPath()
    {
        if (!string.IsNullOrEmpty(OverridePath))
            return OverridePath;
        return Shell.Which("dxc");
    }

    static string StageTarget(ShaderStage stage) => stage switch
    {
        ShaderStage.Vertex => "vs_6_0",
        ShaderStage.Fragment => "ps_6_0",
        ShaderStage.Compute => "cs_6_0",
        _ => throw new InvalidOperationException()
    };

    static string StageDefine(ShaderStage stage) => stage switch
    {
        ShaderStage.Vertex => "VERTEX",
        ShaderStage.Fragment => "FRAGMENT",
        ShaderStage.Compute => "COMPUTE",
        _ => throw new InvalidOperationException()
    };

    static string TextureSpaceDefine(ShaderStage stage) => stage switch
    {
        ShaderStage.Vertex => "-DTEXTURE_SPACE=space0",
        ShaderStage.Fragment => "-DTEXTURE_SPACE=space2",
        _ => throw new InvalidOperationException()
    };

    static string UniformSpaceDefine(ShaderStage stage) => stage switch
    {
        ShaderStage.Vertex => "-DUNIFORM_SPACE=space1",
        ShaderStage.Fragment => "-DUNIFORM_SPACE=space3",
        _ => throw new InvalidOperationException()
    };

    struct TempFile : IDisposable
    {
        public string Path;
        public static TempFile Create() => new() { Path = System.IO.Path.GetTempFileName() };

        public void Dispose()
        {
            try
            {
                File.Delete(Path);
            }
            catch
            {
                // ignored
            }
        }
    }

    static async Task<int> RunWithRetries(string path, params string[] args)
    {
        int retryCount = 0;
        int result = 0;
        while (retryCount++ < 4)
        {
            result = await Shell.Run(path, args);
            if (result != 0)
            {
                await Console.Error.WriteLineAsync($"Invoke {path} failed with code {result}, retrying with 500ms delay.");
                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }
            else
            {
                break;
            }
        }
        return result;
    }

    public static async Task<byte[]> CompileDXIL(string hlslSource, ShaderStage stage)
    {
        var path = GetDXCPath();
        if (path == null)
        {
            throw new Exception("Unable to locate DXC");
        }

        using var input = TempFile.Create();
        using var output = TempFile.Create();

        await File.WriteAllTextAsync(input.Path, hlslSource);

        var result = await RunWithRetries(path,
            "-T",
            StageTarget(stage),
            "-O3",
            "-Fo",
            output.Path,
            input.Path);

        if (result != 0)
        {
            throw new ShaderCompilerException(ShaderError.DXILCompileFailure, "",0,0, "dxc failed compiling transpiled SPIR-V");
        }

        return await File.ReadAllBytesAsync(output.Path);
    }

    public static async Task<string[]> Dependencies(string hlslFile, ShaderStage stage, List<string> defines)
    {
        var path = GetDXCPath();
        if (path == null)
        {
            throw new Exception("Unable to locate DXC");
        }
        var args = new List<string>();
        foreach (var d in defines)
        {
            args.Add("-D");
            args.Add(d);
        }

        args.AddRange([
            "-T",
            StageTarget(stage),
            "-D",
            StageDefine(stage),
            UniformSpaceDefine(stage),
            TextureSpaceDefine(stage),
            "-M",
            hlslFile
        ]);
        var result = await Shell.RunString(path, args.ToArray());
        // Output of -M is like make where it will be `file:` followed by deps,
        // meant to be one line so newlines are escaped. Process like it is shell script.
        var idxColon = result.IndexOf(':');
        var deps = result.Substring(idxColon + 1).Replace("\\\r\n", " ")
            .Replace("\\\n", " ");
        return ParseArgs(deps).ToArray();
    }

    // This may not be correct, check this if it ends up breaking
    private static readonly Regex argsRegex
        = new Regex("([^\\s\"]+\"|((?<=\\s|^)(?!\"\"(?!\"))\")+)(\"\"|.*?)*\"[^\\s\"]*|[^\\s]+",
            RegexOptions.Compiled
            | RegexOptions.Singleline
            | RegexOptions.ExplicitCapture
            | RegexOptions.CultureInvariant);
    internal static IEnumerable<string> ParseArgs(string args) {
        var match = argsRegex.Match(args);

        while (match.Success) {
            yield return match.Value;
            match = match.NextMatch();
        }
    }

    public static async Task<byte[]> CompileSPIRV(string hlslFile, ShaderStage stage, List<string> defines)
    {
        var path = GetDXCPath();
        if (path == null)
        {
            throw new Exception("Unable to locate DXC");
        }

        using var output = TempFile.Create();

        var args = new List<string>();
        foreach (var d in defines)
        {
            args.Add("-D");
            args.Add(d);
        }
        args.AddRange([
            "-T",
            StageTarget(stage),
            "-D",
            StageDefine(stage),
            UniformSpaceDefine(stage),
            TextureSpaceDefine(stage),
            "-O3",
            "-fvk-use-gl-layout", //use std430
            "-fspv-flatten-resource-arrays",
            "-ffinite-math-only", //stops horrific GLSL codegen
            "-spirv",
            "-Zpr", //float4x4 match GL
            "-Fo",
            output.Path,
            hlslFile
        ]);

        var result = await Shell.Run(
            path,
            args.ToArray()
        );

        if (result != 0)
        {
            var defString = defines.Count > 0
                ? $" ({string.Join(", ", defines)})"
                : "";
            throw new ShaderCompilerException(ShaderError.HLSLCompileFailure, hlslFile,0,0,$"Compile failed with defines '{defString}'");
        }
        return await File.ReadAllBytesAsync(output.Path);
    }
}
