namespace ImGuiBindingsGenerator;

public class ProcessedFunction
{
    public string? RemappedName { get; set; }
    public string? EntrypointName { get; set; }
    public FunctionItem Function { get; init; }

    public bool SkipWrapping { get; set; }

    public ProcessedFunction(FunctionItem function)
    {
        Function = function;
    }

    public override string ToString()
    {
        return RemappedName == null
            ? $"F: {Function.Name}"
            : $"R: {RemappedName}";
    }
}

public class ProcessedStruct
{
    public bool IsRefStruct { get; set; }
    public StructItem Struct { get; init; }

    public ProcessedStruct(StructItem structItem)
    {
        Struct = structItem;
    }
}

public class ProcessedDefinitions
{
    public ImGuiDefines Defines;
    public List<ProcessedFunction> Functions = new();
    public List<ProcessedStruct> Structs = new();
    public List<EnumItem> Enums = new();
    public List<TypedefItem> Typedefs = new();

    public List<ReplacementStruct> Replacements;

    public ProcessedDefinitions(JsonDefinitions json, ExtraDefinitions extraDefinitions)
    {
        Defines = new ImGuiDefines();
        foreach (var d in extraDefinitions.Defines)
        {
            Defines.Define(d, "1");
        }
        Defines.AddDefines(json.Defines);

        Dictionary<string, string> redirects = new();
        foreach (var r in extraDefinitions.Redirects)
            redirects[r.Old] = r.New;

        HashSet<string> skips = new();
        foreach (var s in extraDefinitions.Skip)
            skips.Add(s);

        foreach (var cppEnum in json.Enums)
        {
            if (ShouldSkip(Defines, cppEnum.Conditionals))
            {
                Console.WriteLine($"[Exclude] E: {cppEnum.Name}");
                continue;
            }

            var newEnum = cppEnum with
            {
                Elements = cppEnum.Elements.Where(item => !ShouldSkip(Defines, item.Conditionals)).ToList()
            };
            Enums.Add(newEnum);
        }

        foreach (var typeDef in json.Typedefs)
        {
            if (ShouldSkip(Defines, typeDef.Conditionals))
            {
                Console.WriteLine($"[Exclude] T: {typeDef.Name}");
                continue;
            }
            Typedefs.Add(typeDef);
        }

        foreach (var cppFunction in json.Functions)
        {
            if (ShouldSkipFunction(Defines, skips, cppFunction, out var reason))
            {
                Console.WriteLine($"[Exclude] F: {cppFunction.Name} ({reason})");
                continue;
            }

            var processed = new ProcessedFunction(cppFunction);

            if (redirects.TryGetValue(cppFunction.Name, out var ep))
            {
                processed.EntrypointName = ep;
            }

            Functions.Add(processed);
        }

        // Unformatted replace original
        List<ProcessedFunction> toRemove = new();

        foreach (var newFunction in Functions)
        {
            var isUnformattedHelper = newFunction.Function.IsUnformattedHelper ||
                     extraDefinitions.UnformattedHelpers.Contains(newFunction.Function.Name);
            if (isUnformattedHelper &&
                newFunction.Function.Name.EndsWith("Unformatted"))
            {
                var ogName = newFunction.Function.Name.Substring(0,  newFunction.Function.Name.Length - "Unformatted".Length);
                var oldFunction = Functions.FirstOrDefault(x => x.Function.Name == ogName);
                if (oldFunction != null)
                {
                    Console.WriteLine($"[Remap] {oldFunction.Function.Name} -> {newFunction.Function.Name} (unformatted helper)");
                    newFunction.RemappedName = ogName;
                    toRemove.Add(oldFunction);
                }
            }
        }

        foreach (var i in toRemove)
            Functions.Remove(i);

        // Remap overloads

        foreach (var newFunction in Functions)
        {
            if (newFunction.RemappedName != null)
                continue;
            if (extraDefinitions.ManualWrappers.Contains(newFunction.Function.Name))
            {
                newFunction.SkipWrapping = true;
                continue;
            }
            // Reconstruct a "native" name from the original, removing overload data
            var remapName = newFunction.Function.OriginalFullyQualifiedName.Replace("::", "_");
            if (!string.IsNullOrWhiteSpace(newFunction.Function.OriginalClass) &&
                !remapName.StartsWith(newFunction.Function.OriginalClass))
                remapName = $"{newFunction.Function.OriginalClass}_{remapName}";
            if (newFunction.Function.Name == remapName)
                continue;
            // Check overload, warn if none.
            if (Functions.Any(x => x.Function.Name == remapName))
            {
                Console.WriteLine($"[Overload] {remapName} -> {newFunction.Function.Name}");
                newFunction.RemappedName = remapName;
            }
            else
            {
                Console.WriteLine($"[Overload] {remapName} -> {newFunction.Function.Name} (warning: no base function match)");
                newFunction.RemappedName = remapName;
            }
        }



        string[] skipStruct = extraDefinitions.Replacements.Select(x => x.Cpp).ToArray();

        foreach (var cppStruct in json.Structs)
        {
            if (skipStruct.Contains(cppStruct.Name))
            {
                Console.WriteLine($"[Exclude] S: {cppStruct.Name} (remap)");
                continue;
            }

            var members = new List<StructItemField>();
            foreach (var fi in cppStruct.Fields)
            {
                if (ShouldSkip(Defines, fi.Conditionals))
                {
                    Console.WriteLine($"[Exclude] V: {cppStruct.Name}.{fi.Name} (conditionals)");
                    continue;
                }

                members.Add(fi);
            }

            ProcessedStruct ps;
            if (extraDefinitions.ByvalueStructs.Contains(cppStruct.Name))
            {
                Console.WriteLine($"[ByValue] S: {cppStruct.Name} forced on");
                ps= new(cppStruct with { ByValue = true, Fields = members });
            }
            else
            {
                ps = new(cppStruct with { Fields = members });
            }
            if (extraDefinitions.RefStructs.Contains(cppStruct.Name))
            {
                ps.IsRefStruct = true;
            }
            Structs.Add(ps);
        }

        Replacements = extraDefinitions.Replacements;
    }

    static bool ShouldSkip(ImGuiDefines defines, List<ConditionalItem>? conditionals)
    {
        if (conditionals == null)
            return false;
        return !defines.EvalConditionals(conditionals);
    }

    static bool ShouldSkipFunction(ImGuiDefines defines, HashSet<string> skips, FunctionItem function, out string reason)
    {
        reason = "";
        if (ShouldSkip(defines, function.Conditionals))
        {
            reason = "Conditionals";
            return true;
        }

        if (function.IsImstrHelper || function.IsDefaultArgumentHelper)
        {
            reason = "DefaultHelper";
            return true;
        }

        if (skips.Contains(function.Name))
        {
            reason = "Manual Skip";
            return true;
        }

        foreach (var a in function.Arguments)
        {
            if (a.Type?.Declaration == "va_list")
            {
                reason = "va_list";
                return true;
            }
        }
        return false;
    }
}
