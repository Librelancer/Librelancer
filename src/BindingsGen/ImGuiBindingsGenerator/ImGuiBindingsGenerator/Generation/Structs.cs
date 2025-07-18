namespace ImGuiBindingsGenerator.Generation;

public static class Structs
{
    public static void WriteStructs(ProcessedDefinitions definitions, TypeConversions types, 
        StructPtrWrappers structPtrs, string outputDir)
    {
        // Create types
        foreach (var structDef in definitions.Structs)
        {
            if (structDef.IsAnonymous ||
                structDef.Name.StartsWith("ImVector_"))
                continue;
    
            if (structDef.ForwardDeclaration)
            {
                structPtrs.AddStructPtr(structDef.Name);
                types.RegisterForwardDeclaration(structDef.Name);
                continue;
            }

            var cw = new CodeWriter();
            cw.AppendLine(
                "#pragma warning disable CS9084 // Struct member returns 'this' or other instance members by reference");
            cw.AppendLine("using System;");
            cw.AppendLine("using System.Runtime.InteropServices;");
            cw.AppendLine("namespace ImGuiNET;");
            cw.AppendLine();
            WriteStruct(cw, structDef.Name, definitions.Functions, structDef, definitions, types);
   
            File.WriteAllText(Path.Combine(outputDir, "Structs", structDef.Name + ".cs"), cw.ToString());
        }
    }
    
    public static void WriteStruct(
        CodeWriter cw, 
        string structName, 
        List<ProcessedFunction> functions, 
        StructItem structDef,
        ProcessedDefinitions definitions,
        TypeConversions types)
    {
        bool isUnion = structDef.Kind == "union";

        cw.AppendComments(structDef.Comments);
        if (isUnion)
            cw.AppendLine("[StructLayout(LayoutKind.Explicit)]");
        cw.AppendLine($"public unsafe partial struct {structName}");
        int widthAmount = 0;
        int bitfieldID = 0;
        using (cw.Block())
        {
            // Fields
            foreach (var member in structDef.Fields)
            {
                if (member.IsAnonymous)
                {
                    var anonType = definitions.Structs.FirstOrDefault(x => x.Name == member.Type.Declaration);
                    if (anonType == null)
                    {
                        throw new Exception($"Could not find anonymous type {member.Type.Declaration}");
                    }
                    if (anonType.Fields is { Count: > 0 })
                    {
                        var n = structName + "_" + anonType.Name;
                        WriteStruct(cw, n, functions, anonType, definitions, types);
                        cw.AppendComments(member.Comments);
                        if (isUnion)
                            cw.AppendLine("[FieldOffset(0)]");
                        cw.Append("public ")
                            .Append(n)
                            .AppendLine($" {ItemUtilities.FixIdentifier(member.Name)};");
                    }
                }
                else
                {
                    if (member.IsArray)
                    {
                        var type = types.GetConversion(structName, member.Type);
                        cw.AppendLine(
                            $"[System.Runtime.CompilerServices.InlineArray({definitions.Defines.ProcessBounds(member.ArrayBounds!)})]");
                        var ident = ItemUtilities.FixIdentifier(member.Name);
                        cw.AppendLine($"private struct __inline_{ident}");
                        using (cw.Block())
                        {
                            cw.AppendLine($"public {type.ArrayTypeName()} _0;");
                        }

                        cw.AppendLine($"private __inline_{ident} __array_{ident};");
                        cw.AppendComments(member.Comments);
                        cw.AppendLine($"public Span<{type.ArrayTypeName()}> {ident} => __array_{ident};");
                        cw.AppendLine();
                    }
                    else
                    {
                        if (isUnion)
                            cw.AppendLine("[FieldOffset(0)]");
                        var type = types.GetConversion(structName, member.Type);
                        var ident = ItemUtilities.FixIdentifier(member.Name);

                        if (member.Width != 0)
                        {
                            if (widthAmount == 0)
                            {
                                bitfieldID++;
                                cw.AppendLine($"private {type.InteropName} __bitfield{bitfieldID};");
                            }
                            cw.AppendLine($"public {type.InteropName} {ident}");
                            using (cw.Block())
                            {
                                cw.AppendLine($"get => ({type.InteropName})((__bitfield{bitfieldID} >> {widthAmount}) & (0b{new string('1', member.Width)}));");
                            }
                            widthAmount += member.Width;
                        }
                        else if (type.ShouldMakeProperty)
                        {
                            widthAmount = 0;
                            cw.AppendLine($"private {type.InteropName} __{ident};");
                            cw.AppendComments(member.Comments);
                            cw.AppendLine($"public {type.FriendlyName} {ident}");
                            using (cw.Block())
                            {
                                cw.AppendLine($"get => {type.GetToFriendly($"__{ident}")};");
                                cw.AppendLine($"set => __{ident} = {type.GetToInterop("value")};");
                            }
                        }
                        else
                        {
                            widthAmount = 0;
                            cw.AppendComments(member.Comments);
                            cw.AppendLine($"public {type.InteropName} {ident};");
                        }
                    }

                }
            }

            // Methods (for byValue structs)
            if (!structDef.ByValue)
                return;

            foreach (var f in functions)
            {
                if (f.Function.OriginalClass == structDef.Name)
                {
                    FunctionWrappers.WrapFunction(cw, f, types, true);
                }
            }
        }
    }
}