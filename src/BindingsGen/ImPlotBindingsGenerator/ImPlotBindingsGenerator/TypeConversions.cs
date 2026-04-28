namespace ImPlotBindingsGenerator;

public enum TypeKind
{
    Enum,
    Struct,
    Pointer
}
public static class TypeConversions
{
    public static string CToCppCast(string type, string src, Dictionary<string, TypeKind> allTypes, bool castStruct = false)
    {
        if (type.StartsWith("const "))
        {
            var noConst = type.Substring("const ".Length);
            if (allTypes.TryGetValue(noConst.TrimEnd('*'), out var kind))
            {
                if (type.EndsWith("*"))
                    return $"reinterpret_cast<const ::{noConst}>({src})";
                return kind switch
                {
                    TypeKind.Enum => $"static_cast<const ::{noConst}>({src})",
                    TypeKind.Struct when castStruct => $"ConvertToCPP_{noConst}({src})",
                    TypeKind.Struct when !castStruct => src, // ConvertToCPP handled by json file,
                    TypeKind.Pointer => $"reinterpret_cast<const ::{noConst}({src})"
                };
            }
        }
        else
        {
            if (allTypes.TryGetValue(type.TrimEnd('*'), out var kind))
            {
                if (type.EndsWith("*"))
                    return $"reinterpret_cast<::{type}>({src})";
                return kind switch
                {
                    TypeKind.Enum => $"static_cast<::{type}>({src})",
                    TypeKind.Struct when castStruct => $"ConvertToCPP_{type}({src})",
                    TypeKind.Struct when !castStruct => src, // ConvertToCPP handled by json file,
                    TypeKind.Pointer => $"reinterpret_cast<::{type}>({src})"
                };
            }
        }

        return src;
    }

    public static string CppToCCast(string type, string src, Dictionary<string, TypeKind> allTypes)
    {
        if (type.StartsWith("cimgui::"))
        {
            type = type.Substring("cimgui::".Length);
        }
        if (allTypes.TryGetValue(type.TrimEnd('*'), out var kind))
        {
            if (type.EndsWith("*"))
                return $"reinterpret_cast<cimgui::{type}>({src})";
            return kind switch
            {
                TypeKind.Enum => $"static_cast<cimgui::{type}>({src})",
                TypeKind.Struct => $"ConvertFromCPP_{type}({src})",
                TypeKind.Pointer => $"reinterpret_cast<cimgui::{type}>({src})"
            };
        }
        return src;
    }

}
