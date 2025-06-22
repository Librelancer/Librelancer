using System.Text;

namespace LLShaderCompiler;

public static class ShaderCompiler
{
    public static async Task Compile(string inputFile, string outputFile, string dumpFolder, bool verbose)
    {
        var shader = await ShaderInfo.FromFile(inputFile);

        if(verbose)
            Console.WriteLine(inputFile);

        if (!string.IsNullOrWhiteSpace(dumpFolder))
        {
            Directory.CreateDirectory(dumpFolder);
        }

        var permutations = FeatureHelper.AllPermutations(shader.Features.Select(x => x.Mask)).ToArray();
        var allShaders = new BundledShader[permutations.Length];

        await Parallel.ForEachAsync(permutations, async (permutation, _) =>
        {
            List<string> defines = new List<string>();
            foreach (var f in shader.Features)
            {
                if ((permutation & f.Mask) == f.Mask)
                {
                    defines.Add(f.Feature);
                }
            }

            if (verbose)
                Console.WriteLine($"Compiling {(defines.Count > 0 ? string.Join(", ", defines) : "default")}.");

            var variant = await CompileVariantSPIRV(shader, defines);
            var reflected = SpvcReflection.ReflectProgram(variant.Vertex, variant.Fragment);
            var glCompiled = shader.NoLegacy
                ? null
                : GLTranslator.TranslateProgram(shader.FriendlyName, variant.Vertex, variant.Fragment);

            var dxilCompiled = await DXILTranslator.TranslateProgram(reflected);
            
            var mslCompiled = MSLTranslator.TranslateProgram(reflected);

            if (!string.IsNullOrWhiteSpace(dumpFolder))
            {
                var ident = defines.Count > 0
                    ? string.Join(".", defines)
                    : "default";
                ident = Path.GetFileNameWithoutExtension(inputFile) + "." + ident;

                await File.WriteAllBytesAsync(Path.Combine(dumpFolder, $"{ident}.vert.spv"), reflected.Vertex.Code);
                await File.WriteAllBytesAsync(Path.Combine(dumpFolder, $"{ident}.frag.spv"), reflected.Fragment.Code);
                if (dxilCompiled != null)
                {
                    await File.WriteAllBytesAsync(Path.Combine(dumpFolder, $"{ident}.vert.dxil"),
                        dxilCompiled.Vertex.Code);
                    await File.WriteAllBytesAsync(Path.Combine(dumpFolder, $"{ident}.frag.dxil"),
                        dxilCompiled.Fragment.Code);
                }
                //Don't include terminating null in output
                await File.WriteAllTextAsync(Path.Combine(dumpFolder, $"{ident}.vert.msl"),
                    Encoding.UTF8.GetString(mslCompiled.Vertex.Code, 0, mslCompiled.Vertex.Code.Length - 1));
                await File.WriteAllTextAsync(Path.Combine(dumpFolder, $"{ident}.frag.msl"),
                    Encoding.UTF8.GetString(mslCompiled.Fragment.Code, 0, mslCompiled.Fragment.Code.Length - 1));
                if (!shader.NoLegacy)
                {
                    await File.WriteAllTextAsync(Path.Combine(dumpFolder, $"{ident}.vert.glsl"),
                        glCompiled!.VertexSource);
                    await File.WriteAllTextAsync(Path.Combine(dumpFolder, $"{ident}.frag.glsl"),
                        glCompiled!.FragmentSource);
                }
            }

            var idx = Array.IndexOf(permutations, permutation);
            allShaders[idx] = new BundledShader(permutation, reflected, dxilCompiled, mslCompiled, glCompiled);
        });

        if(verbose)
            Console.WriteLine($"Packing output to {outputFile}");
        using var bundle = File.Create(outputFile);
        ShaderBundleWriter.Write(bundle, new ShaderBundle() { Shaders = allShaders });
    }

    static async Task<(byte[] Vertex, byte[] Fragment)> CompileVariantSPIRV(ShaderInfo shader, List<string> defines)
    {
        var vertex = await DXC.CompileSPIRV(shader.VertexSource, ShaderStage.Vertex, defines);
        var fragment = await DXC.CompileSPIRV(shader.FragmentSource, ShaderStage.Fragment, defines);
        return (vertex, fragment);
    }
}
