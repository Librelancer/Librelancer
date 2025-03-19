using LibreLancer.GeneratorCommon;

namespace LibreLancer.Data.Generator;

public record struct ParsedSectionInfo(
    string Namespace,
    string Name,
    bool IsIEntryHandler,
    bool IsBaseSection,
    bool HasBaseSection,
    string? OnParseDependent,
    EquatableArray<Entry> Entries,
    EquatableArray<EntryHandler> Handlers,
    EquatableArray<Section> Children
);

public record struct ParsedIniInfo(
    string Namespace,
    string Name,
    bool Preparse,
    EquatableArray<Section> Sections,
    EquatableArray<string> IgnoreSections
);

public record struct Section(
    string SectionName,
    string FieldName,
    string FieldType,
    string SectionType,
    bool Child,
    bool List,
    EquatableArray<string> Delimiters
    );

public record struct Entry(
    string EntryName,
    string FieldName,
    EntryType Type,
    bool Multiline,
    bool Required,
    bool MinMax,
    bool Presence,
    bool FloatColor,
    Vec3Mode Vec3Mode);

public enum Vec3Mode
{
    None = 0,
    Size = 1,
    OptionalComponents = 2
}

public record struct EntryType(SupportedType Type, bool List, bool Array, string? EnumName)
{
    public static EntryType Basic(SupportedType type) => new(type, false, false, null);
}

public record struct EntryHandler(string EntryName, string MethodName, bool Multiline, int Components);

public enum SupportedType
{
    String,
    Float,
    Boolean,
    Enum,
    Int,
    Long,
    Vector4,
    Vector3,
    Vector2,
    Quaternion,
    Color4,
    Color3f,
    Guid,
    HashValue,
    ValueRangeInt,
    ValueRangeFloat
}
