namespace ImPlotBindingsGenerator;

public static class ShimFunctions
{
    public static void Write(List<FuncDefinition> allFunctions, Dictionary<string, string> typedefs,
        StructsAndEnums structsAndEnums, Dictionary<string, TypeKind> allTypes)
    {
        // Write shims

        using var h_file = new StreamWriter("cimplot.h");
        h_file.WriteLine(@"//auto-generated
#ifndef _CIMGUI_PLOT_H_
#define _CIMGUI_PLOT_H_
#include ""cimgui_ext.h""
#include ""dcimgui_nodefaultargfunctions.h""
#ifdef __cplusplus
extern ""C"" {
#endif

typedef int (*ImPlotFormatter)(double value, char* buff, int size, void* user_data);
typedef double (*ImPlotTransform)(double value, void* user_data);
");



        foreach (var td in typedefs.Where(x => x.Value == "int"))
        {
            h_file.WriteLine($"typedef {td.Value} {td.Key};");
        }

        foreach (var en in structsAndEnums.enums)
        {
            h_file.WriteLine($"enum {en.Key}");
            h_file.WriteLine("{");
            foreach (var m in en.Value)
            {
                h_file.WriteLine($"    {m.name} = {m.value},");
            }
            h_file.WriteLine("};");
        }


        foreach (var s in structsAndEnums.structs)
        {
            if (structsAndEnums.locations.TryGetValue(s.Key, out var loc) &&
                loc.Contains("internal"))
                continue;
            h_file.WriteLine($"struct {s.Key}");
            h_file.WriteLine("{");
            foreach (var f in s.Value)
            {
                h_file.WriteLine($"    {f.type} {f.name};");
            }
            h_file.WriteLine("};");
        }

        foreach (var td in typedefs.Where(x => x.Value != "int"))
        {
            if (td.Value.Contains("(*"))
                continue;
            h_file.WriteLine($"typedef {td.Value} {td.Key};");
        }



        h_file.WriteLine(@"
typedef ImPlotPoint (*ImPlotGetter)(int idx, void* user_data);
");

        using var cpp_file = new StreamWriter("cimplot.cpp");
        cpp_file.WriteLine(@"//auto-generated
#define IMPLOT_DISABLE_OBSOLETE_FUNCTIONS
#include ""imgui.h""
#include ""implot.h""

namespace cimgui
{
    #include ""cimplot.h""
}

static inline ::ImVec2 ConvertToCPP_ImVec2(const cimgui::ImVec2& src)
{
    ::ImVec2 dest;
    dest.x = src.x;
    dest.y = src.y;
    return dest;
}

static inline cimgui::ImVec2 ConvertFromCPP_ImVec2(const ::ImVec2& src)
{
    cimgui::ImVec2 dest;
    dest.x = src.x;
    dest.y = src.y;
    return dest;
}

static inline ::ImVec4 ConvertToCPP_ImVec4(const cimgui::ImVec4& src)
{
    ::ImVec4 dest;
    dest.x = src.x;
    dest.y = src.y;
    dest.z = src.z;
    dest.w = src.w;
    return dest;
}

static inline cimgui::ImVec4 ConvertFromCPP_ImVec4(const ::ImVec4& src)
{
    cimgui::ImVec4 dest;
    dest.x = src.x;
    dest.y = src.y;
    dest.z = src.z;
    dest.w = src.w;
    return dest;
}

static inline ::ImTextureRef ConvertToCPP_ImTextureRef(const cimgui::ImTextureRef& src)
{
    ::ImTextureRef dest;
    dest._TexData = reinterpret_cast<::ImTextureData*>(src._TexData);
    dest._TexID = src._TexID;
    return dest;
}

");

        foreach (var s in structsAndEnums.structs)
        {
            if (structsAndEnums.locations.TryGetValue(s.Key, out var loc) &&
                loc.Contains("internal"))
                continue;
            if (s.Key.Equals("ImPlotStyle") || s.Key.Equals("ImPlotInputMap"))
                continue; // blacklist for conversion
            cpp_file.WriteLine($"static inline ::{s.Key} ConvertToCPP_{s.Key}(const cimgui::{s.Key}& src)");
            cpp_file.WriteLine("{");
            cpp_file.WriteLine($"    ::{s.Key} dest;");
            foreach (var f in s.Value)
            {
                var srcMember = $"src.{f.name}";
                cpp_file.WriteLine($"    dest.{f.name} = {TypeConversions.CToCppCast(f.type, srcMember, allTypes, true)};");
            }
            cpp_file.WriteLine("    return dest;");
            cpp_file.WriteLine("}");


            cpp_file.WriteLine($"static inline cimgui::{s.Key} ConvertFromCPP_{s.Key}(const ::{s.Key}& src)");
            cpp_file.WriteLine("{");
            cpp_file.WriteLine($"    cimgui::{s.Key} dest;");
            foreach (var f in s.Value)
            {
                var srcMember = $"src.{f.name}";
                cpp_file.WriteLine($"    dest.{f.name} = {TypeConversions.CppToCCast(f.type, srcMember, allTypes)};");
            }
            cpp_file.WriteLine("    return dest;");
            cpp_file.WriteLine("}");
        }

        foreach (var f in allFunctions)
        {
            var processedArgs = TypeHandling.CleanTypes(f.args);
            var processedRet = TypeHandling.CleanTypes(f.ret);

            h_file.WriteLine($"CIMGUI_API {processedRet} {f.ov_cimguiname}{processedArgs};");
        }

        foreach (var f in allFunctions)
        {
            var processedArgs = TypeHandling.CleanTypes(f.CppFileArgs);
            var processedRet = TypeHandling.CleanTypes(f.ret);
            if (allTypes.ContainsKey(processedRet.TrimEnd('*')))
                processedRet = $"cimgui::{processedRet}";

            cpp_file.WriteLine($"CIMGUI_API {processedRet} cimgui::{f.ov_cimguiname}{processedArgs}");
            cpp_file.WriteLine("{");
            if(f.ret != "void")
                cpp_file.Write("    return ");
            else
                cpp_file.Write("    ");

            var call = $"{f.@namespace}::{f.funcname}{f.CppFileCall}";

            if (f.retref == "&")
            {
                cpp_file.WriteLine($"reinterpret_cast<{processedRet}>(&{call});");
            }
            else
            {
                cpp_file.Write(TypeConversions.CppToCCast(processedRet, call, allTypes));
                cpp_file.WriteLine(";");
            }
            cpp_file.WriteLine("}");
        }

        h_file.WriteLine(@"

#ifdef __cplusplus
}
#endif
#endif");
    }
}
