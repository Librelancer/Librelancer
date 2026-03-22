namespace ImPlotBindingsGenerator;

public static class ShimFunctions
{
    public static void Write(IEnumerable<FuncDefinition> allFunctions, Dictionary<string, string> typedefs)
    {
        // Write shims

        using var h_file = new StreamWriter("cimplot.h");
        h_file.WriteLine(@"//auto-generated
#ifndef _CIMGUI_PLOT_H_
#define _CIMGUI_PLOT_H_
#include ""cimgui_ext.h""
#ifdef __cplusplus
extern ""C"" {
#endif

typedef int (*ImPlotFormatter)(double value, char* buff, int size, void* user_data);
typedef double (*ImPlotTransform)(double value, void* user_data);

");

        foreach (var td in typedefs)
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
#include ""cimplot.h""
#include ""implot.h""
#define ConvertToCPP_ImVec4(x) (x)
#define ConvertToCPP_ImVec2(x) (x)
#define ConvertToCPP_ImPlotSpec(x) (*x)
#define ConvertToCPP_ImPlotPoint(x) (x)
#define ConvertToCPP_ImPlotRect(x) (x)
#define ConvertToCPP_ImPlotRange(x) (x)
#define ConvertToCPP_ImTextureRef(x) (x)
");

        foreach (var f in allFunctions)
        {
            var processedArgs = TypeHandling.CleanTypes(f.args);
            var processedRet = TypeHandling.CleanTypes(f.ret);
    
            h_file.WriteLine($"CIMGUI_API {processedRet} {f.ov_cimguiname}{processedArgs};");
            cpp_file.WriteLine($"CIMGUI_API {processedRet} {f.ov_cimguiname}{processedArgs}");
            cpp_file.WriteLine("{");
            if(f.ret != "void")
                cpp_file.Write("    return ");
            else
                cpp_file.Write("    ");
            if(f.retref == "&")
                cpp_file.WriteLine($"reinterpret_cast<{processedRet}>(&");
            cpp_file.Write($"{f.@namespace}::{f.funcname}");
            cpp_file.Write($"{f.call_args}");
            if(f.retref == "&")
                cpp_file.Write(")");
            cpp_file.WriteLine(";");
            cpp_file.WriteLine("}");
        }

        h_file.WriteLine(@"

#ifdef __cplusplus
}
#endif
#endif");
    }
}