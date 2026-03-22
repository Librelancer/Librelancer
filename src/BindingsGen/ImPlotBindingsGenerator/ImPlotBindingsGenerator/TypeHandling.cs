namespace ImPlotBindingsGenerator;

public class TypeHandling
{
    public const string TYPE_FORMATTER = "delegate* unmanaged<double, IntPtr, int, void*, int>";
    public const string TYPE_GETTER = "delegate* unmanaged<int, void*, ImPlotPoint>";
    public const string TYPE_TRANSFORM = "delegate* unmanaged<double, void*, double>";
    
    public static string CleanTypes(string typeStr) => typeStr
        .Replace("ImPlotSpec_c", "ImPlotSpec*")
        .Replace("const ImPlotPoint_c", "ImPlotPoint")
        .Replace("ImPlotPoint_c", "ImPlotPoint")
        .Replace("const ImPlotRect_c", "ImPlotRect")
        .Replace("ImPlotRect_c", "ImPlotRect")
        .Replace("ImPlotRange_c", "ImPlotRange")
        .Replace("ImTextureRef_c", "ImTextureRef")
        .Replace("const ImVec2_c", "ImVec2")
        .Replace("const ImVec4_c", "ImVec4")
        .Replace("ImVec2_c", "ImVec2")
        .Replace("ImVec4_c", "ImVec4");
    
    public static string ManagedType(string args) => TypeHandling.CleanTypes(args)
        .Replace("ImVec2", "Vector2")
        .Replace("ImVec4", "Vector4")
        .Replace("const char*", "string")
        .Replace("string const[]", "string[]")
        .Replace("const ImPlotSpec", "ImPlotSpec?")
        .Replace("const ", "")
        .Replace("ImU8", "byte")
        .Replace("ImS8", "sbyte")
        .Replace("ImU16", "ushort")
        .Replace("ImS16", "short")
        .Replace("ImU32", "uint")
        .Replace("ImS32", "int")
        .Replace("ImU64", "ulong")
        .Replace("ImS64", "long")
        .Replace("ImGuiContext*", "IntPtr")
        .Replace("ImPlotContext*", "IntPtr")
        .Replace("bool*", "byte*")
        .Replace("ImPlotFormatter", TYPE_FORMATTER)
        .Replace("ImPlotGetter", TYPE_GETTER)
        .Replace("ImPlotTransform", TYPE_TRANSFORM);
    
    public static string CsNativeTypes(string args) => CleanTypes(args)
        .Replace("ImVec2", "Vector2")
        .Replace("ImVec4", "Vector4")
        .Replace("char*", "byte*")
        .Replace("const ", "")
        .Replace("ImU8", "byte")
        .Replace("ImS8", "sbyte")
        .Replace("ImU16", "ushort")
        .Replace("ImS16", "short")
        .Replace("ImU32", "uint")
        .Replace("ImS32", "int")
        .Replace("ImU64", "ulong")
        .Replace("ImS64", "long")
        .Replace("bool", "byte")
        .Replace("ImPlotContext*", "IntPtr")
        .Replace("byte* label_ids[]", "IntPtr label_ids")
        .Replace("out,", "output,")
        .Replace("ref)", "reference)")
        .Replace("ref,", "reference,")
        .Replace("byte* labels[]", "IntPtr labels")
        .Replace("ImGuiContext*", "IntPtr")
        .Replace("ImPlotFormatter", TYPE_FORMATTER)
        .Replace("ImPlotGetter", TYPE_GETTER)
        .Replace("ImPlotTransform", TYPE_TRANSFORM);
}