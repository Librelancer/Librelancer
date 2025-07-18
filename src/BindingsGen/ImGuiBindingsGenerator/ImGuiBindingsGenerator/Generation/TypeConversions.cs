namespace ImGuiBindingsGenerator.Generation;

public class TypeConversions
{
    private Dictionary<string, TypeConversion> conversions = new();
    public Delegates DelegateTypes;
    private StructPtrWrappers structs;

    public TypeConversions(Delegates delegateTypes, StructPtrWrappers structs)
    {
        DelegateTypes = delegateTypes;
        this.structs = structs;
    }

    public void FillTypes(ProcessedDefinitions definitions)
    {
        // Register builtin aliases
        foreach (var typeDef in definitions.Typedefs)
        {
            if (IsRegistered(typeDef.Name))
                continue;
            if (typeDef.Type.Description.Kind == "Builtin")
            {
                RegisterBuiltin(typeDef.Name, typeDef.Type.Description.BuiltinType!);
            }
        }

        List<StructItem> ImVectors = new();
        // Register struct types
        foreach (var structDef in definitions.Structs)
        {
            if (structDef.Struct.IsAnonymous)
                continue;

            if (structDef.Struct.ForwardDeclaration)
            {
                RegisterForwardDeclaration(structDef.Struct.Name);
            }
            else if (structDef.Struct.Name.StartsWith("ImVector_"))
            {
                ImVectors.Add(structDef.Struct);
            }
            else
            {
                RegisterStruct(structDef.Struct.Name, structDef.Struct.ByValue || structDef.IsRefStruct);
            }
        }

        // Register delegates
        List<(string Name, TypeDescription Function)> collectedDelegates = new();
        foreach (var typeDef in definitions.Typedefs)
        {
            if (IsRegistered(typeDef.Name))
                continue;
            if (typeDef.Type.Description.Kind == "Type" &&
                typeDef.Type.Description.InnerType?.Kind == "Pointer" &&
                typeDef.Type.Description.InnerType?.InnerType?.Kind == "Function")
            {
                collectedDelegates.Add((typeDef.Name, typeDef.Type.Description.InnerType.InnerType!));
            }
        }

        // Register redeclared types
        foreach (var typeDef in definitions.Typedefs)
        {
            if (IsRegistered(typeDef.Name))
                continue;
            if (typeDef.Type.Description.Kind == "User")
            {
                Redeclare(typeDef.Name, typeDef.Type.Description.Name!);
            }
        }

        // Generate delegates
        foreach (var del in collectedDelegates)
        {
            RegisterDelegate(del.Name, del.Name, DelegateTypes.GenerateDelegate(del.Name, this, del.Function));
        }


        foreach (var imvec in ImVectors)
        {
            var dataField = imvec.Fields.First(x => x.Name.Equals("Data"));
            var fieldConv = GetConversion("", dataField.Type.Declaration.Substring(0,
                dataField.Type.Declaration.Length - 1), dataField.Type.Description.InnerType!);
            if (fieldConv.Kind == TypeKind.ForwardDeclaration)
            {
                RegisterImVector(imvec.Name, "ImVector<IntPtr>");
            }
            else if (fieldConv.Kind == TypeKind.Pointer || fieldConv.Kind == TypeKind.WrappedStruct)
            {
                RegisterImVector(imvec.Name, $"ImPtrVector<{fieldConv.InteropName.TrimEnd('*')}>");
            }
            else
            {
                RegisterImVector(imvec.Name, $"ImVector<{fieldConv.InteropName}>");
            }
        }
    }

    public TypeConversion GetConversion(string context, string declaration, TypeDescription description)
    {
        if (description.Kind == "Type")
        {
            return GetConversion(context, declaration, description.InnerType!);
        }

        if (description.Kind == "Array")
        {
            var inner = description.InnerType!;
            var conv = GetConversion(context, inner.Name!, inner);
            if (conv.Kind == TypeKind.String)
                return new StringArrayType();
            return new FixedArrayType(declaration, conv.InteropName, description.Bounds);
        }

        if (description.Kind == "Pointer")
        {
            var inner = description.InnerType!;
            // Function pointer
            if (inner.Kind == "Function")
            {
                var pointerType = DelegateTypes.GenerateDelegate($"{context}Delegate", this, inner);
                return new DelegateType(declaration, $"{context}Delegate", pointerType);
            }

            var conv = GetConversion(context, inner.Name!, inner);
            // Pointers to structs
            if (conv.Kind == TypeKind.ForwardDeclaration)
            {
                return new StructPointer(conv.CppName, "IntPtr", TypeKind.ForwardDeclaration);
            }
            else if (conv.Kind == TypeKind.Struct)
            {
                structs.AddStructPtr(conv.CppName);
                return new StructPointer(conv.CppName, conv.CppName + "*", TypeKind.WrappedStruct);
            }

            // const char *
            if ((inner.StorageClasses?.Contains("const") ?? false)
                && conv.InteropName == "sbyte")
            {
                return TypeConversion.String;
            }

            // char *
            // These will use manual overloads to handle complexity
            if (conv.InteropName == "sbyte")
            {
                return new AliasType("char*", "IntPtr") { Kind = TypeKind.Pointer };
            }

            // Void* (make IntPtr)
            if (conv.Kind == TypeKind.Void)
            {
                return new TypeConversion(conv.CppName + "*", "IntPtr", "IntPtr", "{0}", "{0}", TypeKind.Pointer);
            }

            // Pointers to pointers
            if (conv.Kind == TypeKind.Pointer)
            {
                return new TypeConversion(conv.CppName + "*", conv.InteropName + "*",
                    conv.InteropName + "*", "{0}", "{0}", TypeKind.Pointer);
            }

            if (conv.Kind == TypeKind.String)
            {
                return new AliasType("byte**", "byte**") { Kind = TypeKind.Pointer };
            }

            if (conv.Kind == TypeKind.Function)
            {
                return new PointerType("<pointer to function pointer>",
                    "ref IntPtr",
                    "IntPtr",
                    "IntPtr", "{0}", "{0}");
            }

            // Pointers to basic types
            return new PointerType(
                conv.CppName + "*",
                "ref " + conv.FriendlyName,
                conv.FriendlyName,
                conv.InteropName,
                conv.FriendlyToInterop,
                conv.InteropToFriendly);
        }

        if (description.Kind == "Builtin")
        {
            return BuiltinTypeConversion(declaration, description.BuiltinType!);
        }

        if (conversions.TryGetValue(declaration, out var decl))
        {
            return decl;
        }

        var d = declaration;
        Console.WriteLine($"WARNING: No conversion for {declaration}");
        return new(d, d, d, "{0}", "{0}", TypeKind.Alias);
    }

    public void RegisterImVector(string cppType, string csType)
    {
        conversions[cppType] = new AliasType(cppType, csType, TypeKind.ImVector);
    }

    public TypeConversion GetConversion(string context, TypeItem ti)
    {
        return GetConversion(context, ti.Declaration, ti.Description);
    }

    public void RegisterEnum(string cppEnumType, string csEnumType)
    {
        conversions[cppEnumType] = new AliasType(cppEnumType, csEnumType) { Kind = TypeKind.Enum };
    }

    public void RegisterAlias(string cppEnumType, string csEnumType)
    {
        conversions[cppEnumType] = new AliasType(cppEnumType, csEnumType);
    }

    public void RegisterForwardDeclaration(string cppType)
    {
        conversions[cppType] = new AliasType(cppType, cppType, TypeKind.ForwardDeclaration);
    }

    public void RegisterStruct(string cppType, bool byvalue)
    {
        conversions[cppType] = new AliasType(cppType, cppType, byvalue ? TypeKind.ValueStruct : TypeKind.Struct);
    }

    public bool IsRegistered(string cppType) => conversions.ContainsKey(cppType);


    enum BuiltinTypes
    {
        Unsigned_long_long,
        Unsigned_int,
        Unsigned_short,
        Unsigned_char,
        long_long,
        Int,
        Short,
        Char,
        Double,
        Float,
        Bool,
        Void,
    }

    public static string GetEnumBaseType(string builtinName)
    {
        if (!Enum.TryParse<BuiltinTypes>(builtinName, true, out var builtinType))
            throw new Exception($"Unhandled builtin_type {builtinName}");
        return builtinType switch
        {
            BuiltinTypes.Unsigned_long_long => "ulong",
            BuiltinTypes.Unsigned_int => "uint",
            BuiltinTypes.Unsigned_short => "ushort",
            BuiltinTypes.Unsigned_char => "char",
            BuiltinTypes.long_long => "long",
            BuiltinTypes.Int => "int",
            BuiltinTypes.Short => "short",
            BuiltinTypes.Char => "char",
            _ => "int"
        };
    }

    static TypeConversion BuiltinTypeConversion(string name, string builtinName)
    {
        if (!Enum.TryParse<BuiltinTypes>(builtinName, true, out var builtinType))
            throw new Exception($"Unhandled builtin_type {builtinName}");
        return builtinType switch
        {
            BuiltinTypes.Unsigned_long_long => new AliasType(name, "ulong"),
            BuiltinTypes.Unsigned_int => new AliasType(name, "uint"),
            BuiltinTypes.Unsigned_short => new AliasType(name, "ushort"),
            BuiltinTypes.Unsigned_char => new AliasType("name", "byte"),
            BuiltinTypes.long_long => new AliasType(name, "long"),
            BuiltinTypes.Int => new AliasType(name, "int"),
            BuiltinTypes.Short => new AliasType(name, "short"),
            BuiltinTypes.Char => new AliasType(name, "sbyte"),
            BuiltinTypes.Double => new AliasType(name, "double"),
            BuiltinTypes.Float => new AliasType(name, "float"),
            BuiltinTypes.Bool => new BooleanType(name),
            BuiltinTypes.Void => TypeConversion.Void,
            _ => throw new InvalidOperationException()
        };
    }

    public void RegisterDelegate(string cppName, string csharpName, string pointerType)
    {
        conversions[cppName] = new DelegateType(cppName, csharpName, pointerType);
    }

    public void Redeclare(string cppName, string oldName)
    {
        var redeclared = conversions[oldName] with { CppName = cppName };
        conversions[cppName] = redeclared;
    }

    public void RegisterBuiltin(string cppName, string builtinName)
    {
        conversions[cppName] = BuiltinTypeConversion(cppName, builtinName);
    }
}

public enum TypeKind
{
    Alias,
    FixedArray,
    Void,
    Enum,
    ForwardDeclaration,
    ValueStruct,
    Struct,
    WrappedStruct,
    Pointer,
    Function,
    ImVector,
    String,
    StringArray,
}

public record TypeConversion(
    string CppName,
    string FriendlyName,
    string InteropName,
    string FriendlyToInterop,
    string InteropToFriendly,
    TypeKind Kind)
{
    public static readonly TypeConversion Void = new AliasType("void", "void") { Kind = TypeKind.Void };
    public static readonly TypeConversion String = new StringType();
    public virtual bool ShouldMakeProperty => false;

    public virtual bool ParameterConversionStart(CodeWriter cw, string ident)
    {
        return false;
    }

    public virtual void ParameterConversionFinally(CodeWriter cw, string ident)
    {
    }

    public virtual void ParameterConversionEnd(CodeWriter cw, string ident)
    {
    }

    public virtual string GetToInterop(string ident) =>
        string.Format(FriendlyToInterop, ident);

    public virtual string GetToFriendly(string ident) =>
        string.Format(InteropToFriendly, ident);

    public virtual string ArrayTypeName() => InteropName;
}

public record StructPointer(string CppName, string PointerType, TypeKind Kind) :
    TypeConversion(CppName, CppName + "Ptr", PointerType, $"{CppName}Ptr.GetHandle({{0}})",
        $"{CppName}Ptr.Create({{0}})", Kind)
{
    public override bool ShouldMakeProperty => true;
}

public record AliasType(string CppName, string CsName, TypeKind Kind = TypeKind.Alias) :
    TypeConversion(CppName, CsName, CsName, "{0}", "{0}", Kind)
{
}


public record StringArrayType() :
    TypeConversion("string[]", $"string[]", "byte**", "__array_{0}", "{0}", TypeKind.StringArray)
{
    public override bool ParameterConversionStart(CodeWriter cw, string ident)
    {
        cw.AppendLine($"byte** __array_{ident} = stackalloc byte*[{ident}.Length];");
        cw.AppendLine($"byte* __storage_{ident} = stackalloc byte[1024];");
        cw.AppendLine(
            $"using var __utf8z_{ident} = new UTF8ZArrayHelper(__storage_{ident}, 1024, __array_{ident}, {ident});");
        return false;
    }
}

public record FixedArrayType(string CppName, string ElementType, string? Bounds) :
    TypeConversion(CppName, $"{ElementType}*", $"{ElementType}*", "{0}", "{0}", TypeKind.FixedArray)
{
    public string ElementType { get; init; } = ElementType;
    public string? Bounds { get; init; } = Bounds;

    public override string ArrayTypeName() => ElementType;

    PointerType ToStructInput(string structName) => new
    (
        CppName,
        "ref " + structName,
        structName,
        ElementType,
        "{0}",
        "{0}"
    );

    public TypeConversion AsRefParameter()
    {
        if (ElementType == "float")
        {
            if (Bounds == "2")
            {
                return ToStructInput("System.Numerics.Vector2");
            }
            else if (Bounds == "3")
            {
                return ToStructInput("System.Numerics.Vector3");
            }
            else if (Bounds == "4")
            {
                return ToStructInput("System.Numerics.Vector4");
            }
        }

        return this;
    }
}

public record BooleanType(string CppName) :
    TypeConversion(CppName, "bool", "byte", "{0} ? (byte)1 : (byte)0", "{0} != 0", TypeKind.Alias)
{
    public override bool ShouldMakeProperty => true;
}

public record DelegateType(string CppName, string FriendlyName, string InteropName) :
    TypeConversion(CppName, FriendlyName, InteropName, "__{0}_p", "{0}", TypeKind.Function)
{
    public override bool ParameterConversionStart(CodeWriter cw, string ident)
    {
        cw.AppendLine($"var __{ident}_p = {ident} == null ? null : ({InteropName})Marshal.GetFunctionPointerForDelegate({ident});");
        return true;
    }

    public override void ParameterConversionFinally(CodeWriter cw, string ident)
    {
        cw.AppendLine($"GC.KeepAlive({ident});");
    }
}

public record PointerType(
    string CppName,
    string FriendlyName,
    string FriendlyBaseName,
    string InteropBaseName,
    string FriendlyToInterop,
    string InteropToFriendly) :
    TypeConversion(CppName, FriendlyName, $"{InteropBaseName}*", FriendlyToInterop, InteropToFriendly, TypeKind.Pointer)
{
    private bool NeedsConversion => FriendlyToInterop != "{0}" || InteropToFriendly != "{0}";

    public override bool ParameterConversionStart(CodeWriter cw, string ident)
    {
        if (NeedsConversion)
        {
            // Converted ref parameter
            cw.AppendLine($"{InteropBaseName} __{ident}_v = {base.GetToInterop(ident)};");
        }
        else if (FriendlyBaseName != InteropBaseName)
        {
            // Cast pointer
            cw.AppendLine($"fixed({FriendlyBaseName}* __{ident}_p2 = &{ident})");
            cw.AppendLine("{").Indent();
            cw.AppendLine($"var __{ident}_p = ({InteropBaseName}*)__{ident}_p2;");
        }
        else
        {
            // Direct pointer
            cw.AppendLine($"fixed({InteropName} __{ident}_p = &{ident})");
            cw.AppendLine("{").Indent();
        }

        return NeedsConversion;
    }

    public override string GetToInterop(string ident)
    {
        if (NeedsConversion)
            return $"&__{ident}_v";
        else
            return $"__{ident}_p";
    }

    public override string GetToFriendly(string ident)
    {
        return $"ref Unsafe.AsRef<{FriendlyBaseName}>(({InteropName}){ident})";
    }

    public override void ParameterConversionFinally(CodeWriter cw, string ident)
    {
        if (NeedsConversion)
        {
            cw.AppendLine($"{ident} = {base.GetToFriendly($"__{ident}_v")};");
        }
    }

    public override void ParameterConversionEnd(CodeWriter cw, string ident)
    {
        if (!NeedsConversion)
        {
            cw.UnIndent().AppendLine("}");
        }
    }
}

public record StringType() : TypeConversion
    ("const char*", "string", "byte*", "__utf8z_{0}.Pointer", "Marshal.PtrToStringUTF8((IntPtr){0})", TypeKind.String)
{
    public override bool ParameterConversionStart(CodeWriter cw, string ident)
    {
        cw.AppendLine($"byte* __bytes_{ident} = stackalloc byte[128];");
        cw.AppendLine($"using var __utf8z_{ident} = new UTF8ZHelper(__bytes_{ident}, 128, {ident});");
        return false;
    }

    public override void ParameterConversionEnd(CodeWriter cw, string ident)
    {
    }
}

public record TextEnd(string TextName) : TypeConversion
    ("const char*", "int?", "byte*", $"__utf8z_{TextName}.GetTextEnd({{0}})", "", TypeKind.String)
{
    public string TextName { get; init; } = TextName;
}
