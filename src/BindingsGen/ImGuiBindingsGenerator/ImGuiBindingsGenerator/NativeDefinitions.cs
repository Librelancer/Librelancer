namespace ImGuiBindingsGenerator;

public record JsonDefinitions(
    List<DefineItem> Defines,
    List<EnumItem> Enums,
    List<TypedefItem> Typedefs,
    List<StructItem> Structs,
    List<FunctionItem> Functions
);

public record FunctionItem(
    string Name,
    string OriginalFullyQualifiedName,
    string OriginalClass,
    TypeItem? ReturnType,
    List<FunctionArgument> Arguments,
    bool IsDefaultArgumentHelper,
    bool IsManualHelper,
    bool IsImstrHelper,
    bool HasImstrHelper,
    bool IsUnformattedHelper,
    Comments? Comments,
    List<ConditionalItem>? Conditionals,
    bool IsInternal
);

public record FunctionArgument(
    string Name,
    TypeItem? Type,
    bool IsArray,
    bool IsVarargs,
    string? DefaultValue,
    bool IsInstancePointer
);

public record DefineItem(
    string Name,
    string? Content,
    List<ConditionalItem>? Conditionals,
    Comments? Comments
);

public record StructItem(
    string Name,
    string OriginalFullyQualifiedName,
    string Kind,
    bool ByValue,
    bool ForwardDeclaration,
    bool IsAnonymous,
    List<StructItemField> Fields,
    Comments? Comments,
    bool IsInternal
);

public record StructItemField(
    string Name,
    bool IsArray,
    bool IsAnonymous,
    string? ArrayBounds,
    Comments? Comments,
    TypeItem Type,
    List<ConditionalItem> Conditionals,
    bool IsInternal,
    int Width
);

public record TypeItem(
    string Declaration,
    TypeDescription Description
);

public record TypeDescription(
    string Kind,
    string? Name,
    string? BuiltinType,
    string? Bounds,
    TypeDescription? ReturnType,
    TypeDescription? InnerType,
    List<TypeDescription>? Parameters,
    List<string> StorageClasses
);

public record TypedefItem(
    string Name,
    TypedefType Type,
    Comments Comments,
    List<ConditionalItem>? Conditionals);

public record TypedefType(
    string Declaration,
    TypeDescription Description,
    TypedefTypeDetails TypeDetails
);

public record TypedefTypeDetails(
    string Flavour,
    TypedefTypeDetailsReturnType ReturnType
);

public record TypedefTypeDetailsReturnType(
    string Declaration,
    TypedefTypeDetailsReturnTypeDescription Description
);

public record TypedefTypeDetailsReturnTypeDescription();

public record TypedefTypeDescription(
    string Kind,
    string BuiltinType);

public record EnumItem(
    string Name,
    string OriginalFullyQualifiedName,
    bool IsFlagsEnum,
    List<EnumElement> Elements,
    List<ConditionalItem> Conditionals,
    Comments? Comments
);

public record EnumElement(
    string Name,
    string ValueExpression,
    int Value,
    bool IsCount,
    bool IsInternal,
    Comments Comments,
    List<ConditionalItem> Conditionals
);

public record Comments(string? Attached, string[]? Preceding);

public record ConditionalItem(
    string Condition,
    string Expression);