namespace ImGuiBindingsGenerator.Generation;

public class StructPtrWrappers
{
    private HashSet<string> generatedStructs = new();
    private List<ProcessedStruct> structs;
    private string outputPath;

    private List<ProcessedStruct> toGenerate = new();

    public StructPtrWrappers(List<ProcessedStruct> structs, string outputPath)
    {
        this.structs = structs;
        this.outputPath = outputPath;
    }

    public void AddStructPtr(string structName)
    {
        if (!generatedStructs.Add(structName))
            return;
        toGenerate.Add(structs.First(x => x.Struct.Name == structName));
    }

    void WriteStructPtr(StructItem si, TypeConversions types, List<ProcessedFunction> functions)
    {
        string pointerType = si.ForwardDeclaration ? "IntPtr" : $"{si.Name}*";
        string nullPtr = si.ForwardDeclaration ? "IntPtr.Zero" : "null";
        var cw = new CodeWriter();
        cw.AppendLine("using System;");
        cw.AppendLine("using System.Runtime.CompilerServices;");
        cw.AppendLine("using System.Runtime.InteropServices;");
        cw.AppendLine();
        cw.AppendLine("namespace ImGuiNET;");
        cw.AppendLine();
        cw.AppendLine($"public unsafe partial class {si.Name}Ptr");
        using (cw.Block())
        {
            cw.AppendLine($"public {pointerType} Handle {{ get; private set; }}");
            cw.AppendLine();

            cw.AppendLine($"public {si.Name}Ptr ({pointerType} handle)");
            using (cw.Block())
            {
                cw.AppendLine("Handle = handle;");
            }
            cw.AppendLine();

            cw.AppendLine($"internal static {si.Name}Ptr Create({pointerType} handle)");
            using (cw.Block())
            {
                cw.AppendLine($"return handle == {nullPtr} ? null : new(handle);");
            }
            cw.AppendLine();

            cw.AppendLine($"internal static {pointerType} GetHandle({si.Name}Ptr self)");
            using (cw.Block())
            {
                cw.AppendLine($"return self == null ? {nullPtr} : self.Handle;");
            }
            cw.AppendLine();

            foreach (var member in si.Fields)
            {
                if (member.IsArray)
                {
                    var type = types.GetConversion($"{si.Name}_{member.Name}", member.Type);
                    cw.AppendComments(member.Comments);
                    var ident = ItemUtilities.FixIdentifier(member.Name);
                    cw.AppendLine($"public Span<{type.ArrayTypeName()}> {ident} => Handle->{ident};");
                }
                else if (member.IsAnonymous)
                {
                    // Do something useful here I guess (probably not needed for now).
                }
                else
                {
                    var type = types.GetConversion($"{si.Name}_{member.Name}", member.Type);
                    var ident = ItemUtilities.FixIdentifier(member.Name);
                    if (type.ShouldMakeProperty || member.Width != 0)
                    {
                        cw.AppendComments(member.Comments);
                        cw.AppendLine($"public {type.FriendlyName} {ident}");
                        using (cw.Block())
                        {
                            cw.AppendLine($"get => Handle->{ident};");
                            if (member.Width == 0)
                            {
                                // Unimplemented : setting bitfields
                                cw.AppendLine($"set => Handle->{ident} = value;");
                            }
                        }
                    }
                    else if (type.Kind == TypeKind.Pointer ||
                             type.Kind == TypeKind.Function ||
                             type.Kind == TypeKind.String ||
                             type.InteropName == "void*")
                    {
                        cw.AppendComments(member.Comments);
                        cw.AppendLine($"public {type.InteropName} {ident}");
                        using (cw.Block())
                        {
                            cw.AppendLine($"get => Handle->{ident};");
                            cw.AppendLine($"set => Handle->{ident} = value;");
                        }
                    }
                    else
                    {
                        cw.AppendComments(member.Comments);
                        cw.AppendLine($"public ref {type.InteropName} {ident} => ref Unsafe.AsRef<{type.InteropName}>(&Handle->{ident});");
                    }
                }
                cw.AppendLine();
            }

            foreach (var f in functions)
            {
                if (f.Function.OriginalClass == si.Name &&
                    !f.SkipWrapping)
                {
                    FunctionWrappers.WrapFunction(cw, f, types);
                }
            }
        }


        File.WriteAllText(Path.Combine(outputPath, "Wrappers", $"{si.Name}Ptr.cs"), cw.ToString());
    }

    public void GenerateWrappers(TypeConversions types, List<ProcessedFunction> functions)
    {
        foreach (var si in toGenerate)
            WriteStructPtr(si.Struct, types, functions);
    }
}
