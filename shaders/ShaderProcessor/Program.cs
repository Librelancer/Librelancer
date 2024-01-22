// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mono.Options;

namespace ShaderProcessor;

public class CodeGenOptions
{
    public bool Brotli;
    public string[] Imports;
    public bool Log = true;
    public string LogMethod = "ShaderLogger.Log";
    public string Namespace = "Namespace";
    public string OutputDirectory;
    public bool Public = true;
    public string ShaderCompileMethod = "Shader.Compile";
    public string ShaderType = "Shader";
    public string GL430Check;
    public string DeviceParameter;
}

internal class Program
{
    private static int Main(string[] args)
    {
        var shouldShowHelp = false;
        var codeOpts = new CodeGenOptions();
        var imports = new List<string>();

        var options = new OptionSet
        {
            {"o|output=", "output directory", n => codeOpts.OutputDirectory = n},
            {"g|gl430check=", "gl430 check expression", g => codeOpts.GL430Check = g},
            {"b|brotli", "compress with brotli (.NET Core only)", b => codeOpts.Brotli = b != null},
            {"l|log", "generate logging code", l => codeOpts.Log = l != null},
            {"x|logmethod=", "logging method name", x => codeOpts.LogMethod = x.Trim()},
            {"t|type=", "shader type name", t => codeOpts.ShaderType = t.Trim()},
            {"i|import=", "import namespace", i => imports.Add(i.Trim())},
            {"n|namespace=", "generated code namespace", n => codeOpts.Namespace = n.Trim()},
            {"c|compilemethod=", "shader compile method name", c => codeOpts.ShaderCompileMethod = c.Trim()},
            {"p|private", "generate internal classes", p => codeOpts.Public = p == null},
            {"h|help", "show this message and exit", h => shouldShowHelp = h != null},
            {"d|device=", "device parameter", d => codeOpts.DeviceParameter = d.Trim()}
        };
        List<string> input = null;
        try
        {
            input = options.Parse(args);
        }
        catch (OptionException e)
        {
            Console.Write("shaderprocessor: ");
            Console.WriteLine(e.Message);
            Console.WriteLine("Try `shaderprocessor --help' for more information.");
            return 1;
        }

        if (shouldShowHelp || input?.Count < 1)
        {
            WriteHelp(options);
            return 0;
        }

        if (string.IsNullOrEmpty(codeOpts.OutputDirectory))
        {
            Console.WriteLine("Output directory must be specified.");
            Console.WriteLine("Try `shaderprocessor --help' for more information.");
            return 2;
        }

        foreach (var i in input)
            if (!File.Exists(i))
            {
                Console.Error.WriteLine($"File does not exist {i}");
                return 2;
            }

        codeOpts.Imports = imports.ToArray();
        var effects = new List<EffectFile>();
        foreach (var i in input)
        {
            var fx = EffectFile.Read(i);
            if (fx == null)
            {
                Console.Error.WriteLine($"Error reading file {i}");
                return 1;
            }

            effects.Add(fx);
        }

        //Don't rely on filesystem sorting, makes merges less crappy
        effects.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));

        Generate(codeOpts, effects);
        return 0;
    }

    private static string InsertDefine(string shader, string version, IEnumerable<string> defs)
    {
        var builder = new StringBuilder();
        builder.Append("#version ").AppendLine(version);
        foreach (var def in defs)
            builder.Append("#define ").AppendLine(def);
        builder.AppendLine("#line 1");
        builder.Append(shader);
        return builder.ToString();
    }

    private static void WriteHelp(OptionSet options)
    {
        Console.WriteLine("Usage: shaderprocessor [OPTIONS]+ -o output inputs");
        Console.WriteLine("Creates C# source from glsl effects");
        Console.WriteLine();
        Console.WriteLine("Options: ");
        options.WriteOptionDescriptions(Console.Out);
    }

    private static void Generate(CodeGenOptions options, IEnumerable<EffectFile> inputs)
    {
        ShaderCompiler.SHInit();
        if (!Directory.Exists(options.OutputDirectory))
            Directory.CreateDirectory(options.OutputDirectory);
        var vals = new List<string>();
        foreach (var input in inputs)
        foreach (var f in input.Features)
            if (!vals.Contains(f))
                vals.Add(f);

        vals.Sort();
        var enumVals = new Dictionary<string, int>();
        for (var i = 0; i < vals.Count; i++) enumVals.Add(vals[i], 1 << i);

        var codeBuilder = new StringBuilder();
        Dictionary<string, int> offsets = new Dictionary<string, int>();
        foreach (var input in inputs)
            File.WriteAllText(
                Path.Combine(options.OutputDirectory, $"{input.Name}.cs"),
                CodeGenerator.Generate(options, codeBuilder, offsets, input, enumVals),
                Encoding.UTF8
            );

        File.WriteAllText(
            Path.Combine(options.OutputDirectory, "ShaderFeatures.cs"),
            CodeGenerator.CreateEnum(options, vals),
            Encoding.UTF8
        );

        File.WriteAllText(
            Path.Combine(options.OutputDirectory, "ShCompHelper.cs"),
            DecompressTemplate.Text.Replace("{NAMESPACE}", options.Namespace)
                .Replace("{COMPRESS}", options.Brotli ? "Brotli" : "Deflate"),
            Encoding.UTF8
        );
        File.WriteAllText(
            Path.Combine(options.OutputDirectory, "AllShaders.cs"),
            CodeGenerator.AllShaders(options, codeBuilder, inputs),
            Encoding.UTF8
        );
        ShaderCompiler.SHFinish();
    }
}
