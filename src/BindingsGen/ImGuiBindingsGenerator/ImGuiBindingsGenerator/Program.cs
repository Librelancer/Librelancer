using System.Text.Json;
using ImGuiBindingsGenerator;
using ImGuiBindingsGenerator.Generation;


Console.WriteLine("ImGuiBindingsGenerator");

if (args.Length > 2)
{
    Console.WriteLine("args: input.json output-dir/");
}
string InputJson = args[0];
string OutputDir = args[1];

T ReadJsonFile<T>(string fileName)
{
    using var fs = File.OpenRead(fileName);
    return JsonSerializer.Deserialize<T>(
        fs,
        new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        }
    )!;
}

var definitions = new ProcessedDefinitions(
    ReadJsonFile<JsonDefinitions>("dcimgui_nodefaultargfunctions.json"),
    ReadJsonFile<ExtraDefinitions>("extradata.json")
);

var delegates = new Delegates();
var structPtrs = new StructPtrWrappers(definitions.Structs, OutputDir);
var types = new TypeConversions(delegates, structPtrs);

types.RegisterAlias("size_t", "nint");

foreach (var replacement in definitions.Replacements)
{
    types.RegisterAlias(replacement.Cpp, replacement.Cs);
}

Directory.CreateDirectory(OutputDir);
Directory.CreateDirectory(Path.Combine(OutputDir, "Enums"));
Directory.CreateDirectory(Path.Combine(OutputDir, "Structs"));
Directory.CreateDirectory(Path.Combine(OutputDir, "Wrappers"));

foreach (var cppEnum in definitions.Enums)
{
    Enums.WriteEnum(cppEnum, definitions, types, OutputDir);
}

types.FillTypes(definitions);

Structs.WriteStructs(definitions, types, structPtrs, OutputDir);
NativeFunctions.Write(definitions, types, OutputDir);
delegates.WriteFile(OutputDir);
FunctionWrappers.WriteImGui(definitions, types, OutputDir);
structPtrs.GenerateWrappers(types, definitions.Functions);
File.Copy("ImVector.cs.txt", Path.Combine(OutputDir, "ImVector.cs"), true);
File.Copy("UTF8Z.cs.txt", Path.Combine(OutputDir, "UTF8Z.cs"), true);
File.Copy("ImOptionalArg.cs.txt", Path.Combine(OutputDir, "ImOptionalArg.cs"), true);

Console.WriteLine();
