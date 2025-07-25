using System.Runtime.InteropServices;
using LLShaderCompiler;

string dxcStr = "";
string dumpFolder = "";
bool help = false;
bool verbose = false;
bool listDeps = false;
var aparse = new Args("Usage: LLShaderCompiler input output");
aparse.String("dxc", "Path to the dxc compiler executable", x => dxcStr = x);
aparse.String("dump", "Folder to dump generated SPIR-V/GLSL.", x => dumpFolder = x);
aparse.Flag("list-deps", "Lists all input file dependencies to stdout.", c => { listDeps = true;
    c.MinArgs = 1;
});
aparse.Flag("verbose", "Enable verbose output.", () => verbose = true);
aparse.Flag("help", "Prints this message.", () => help = true);

var positional = aparse.ParseArgs(args, 2);
DXC.OverridePath = dxcStr;

if (help)
{
    aparse.PrintUsage(Console.Out);
    Environment.Exit(0);
}




if (listDeps)
{
    try
    {
        string[][] allDeps = new string[positional.Length][];

        await Parallel.ForEachAsync(positional, async (file, _) =>
        {
            var selfSet = new HashSet<string>();
            selfSet.Add(Path.GetFullPath(file));

            var shader = await ShaderInfo.FromFile(file);
            var permutations = FeatureHelper.AllPermutations(shader.Features.Select(x => x.Mask)).ToArray();
            List<Task<string[]>> deps = new List<Task<string[]>>();

            foreach (var permutation in permutations)
            {
                deps.Add(Task.Run(async () =>
                {
                    List<string> defines = new List<string>();
                    foreach (var f in shader.Features)
                    {
                        if ((permutation & f.Mask) == f.Mask)
                        {
                            defines.Add(f.Feature);
                        }
                    }

                    var vertexDeps = await DXC.Dependencies(shader.VertexSource, ShaderStage.Vertex, defines);
                    var fragmentDeps = await DXC.Dependencies(shader.FragmentSource, ShaderStage.Fragment, defines);

                    return vertexDeps.Concat(fragmentDeps).ToArray();
                }));
            }

            var shaderResult = await Task.WhenAll(deps);

            foreach (var r in shaderResult)
            {
                foreach (var v in r)
                {
                    selfSet.Add(v);
                }
            }

            var idx = Array.IndexOf(positional, file);
            allDeps[idx] = selfSet.ToArray();
        });

        var totalSet = new HashSet<string>();
        foreach (var r in allDeps)
        {
            foreach (var d in r)
            {
                totalSet.Add(d);
            }
        }

        foreach (var x in totalSet.Order())
        {
            Console.WriteLine(x);
        }

        return;
    }
    catch (ShaderCompilerException e)
    {
        Console.Error.WriteLine(e.ToDiagnosticString());
        Environment.Exit(1);
    }
}


if (positional.Length > 2 && positional.Length % 2 != 0)
{
    Console.Error.WriteLine("Need equal number of inputs and outputs");
    Environment.Exit(1);
}

var files = positional.Chunk(2).Select(x => (x[0], x[1]));
try
{
    await Parallel.ForEachAsync(files, async (f, _) =>
    {
        await ShaderCompiler.Compile(f.Item1, f.Item2, dumpFolder, verbose);
    });
}
catch (ShaderCompilerException e)
{
    Console.Error.WriteLine(e.ToDiagnosticString());
    Environment.Exit(1);
}

if (verbose)
    Console.WriteLine("Done");
