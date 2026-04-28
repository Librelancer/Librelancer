
using System.Text;

namespace ImPlotBindingsGenerator;

public class ManagedFunctions
{
    static string Return(string csRet, string args, bool useRetVal)
    {
        var r = useRetVal ? "retval =" : "return";
        if (csRet == "bool")
            return $"        {r} {args} != 0;";
        else if (csRet == "string")
            return $"        {r} Marshal.PtrToStringUTF8((IntPtr){args});";
        else if (csRet != "void")
            return $"        {r} {args};";
        return $"        {args};";
    }

    static string Name(string a) => a switch
    {
        "out" => "output",
        "ref" => "reference",
        _ => a
    };

    static string ConvertArg(string type, string arg, string? def) => type switch
    {
        "bool" => $"{Name(arg)} ? (byte)1 : (byte)0", //bool->type
        "Vector2" when def != null && def.StartsWith("ImVec2") => $"{Name(arg)} ?? {def.Replace("ImVec2", "new")}",
        "Vector4" when def != null && def.StartsWith("ImVec4") => $"{Name(arg)} ?? {def.Replace("ImVec4", "new")}",
        "ImPlotPoint" when def != null && def.StartsWith("ImPlotPoint") => $"{Name(arg)} ?? {def.Replace("ImPlotPoint", "new")}",
        "string" => $"{Name(arg)} == null ? null : {Name(arg)}_ptr",
        "string[]" => $"{Name(arg)}_u8.Pointer",
        "ImPlotSpec?" => $"{Name(arg)}_spec",
        _ => Name(arg)
    };

    static string ConvertDefault(string type, string def, HashSet<string> genEnums, Dictionary<string, string> enumValues)
    {
        if (enumValues.TryGetValue(def, out var enumVal))
        {
            if (type == "int")
                return $"(int){enumVal}";
            return enumVal;
        }

        if (genEnums.Contains(type))
        {
            if (def == "0")
                return def;
            return def == "IMPLOT_AUTO" ? $"({type})(-1)" : $"({type})({def})";
        }

        return def switch
        {
            "nullptr" when type == "IntPtr" => "default",
            "nullptr" => "null",
            "ImPlotSpec()" => "null",
            "ImPlotRange()" => "default",
            "ImPlotRect()" => "default",
            "IMPLOT_AUTO" => "-1",
            "ImVec2(0,0)" => "default",
            "ImPlotPoint(0,0)" => "default",
            _ => def
        };
    }

    static ManagedFunction ConvertFunction(FuncDefinition f, HashSet<string> genEnums,
        Dictionary<string, string> enumValues)
    {
        var returnType = TypeHandling.ManagedType(f.ret);
        var argTypes = f.signature.Substring(1, f.signature.Length - 2)
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.TrimEntries);
        string[] callValues = new string[f.argsT.Length];
        ManagedArgument[] arguments = new ManagedArgument[f.argsT.Length];
        for (int i = 0; i < f.argsT.Length; i++)
        {
            var t = TypeHandling.ManagedType(argTypes[i]);
            if (f.defaults.TryGetValue(f.argsT[i].name, out var def))
                def = ConvertDefault(TypeHandling.ManagedType(argTypes[i]), def, genEnums, enumValues);
            if (def != null && (def.StartsWith("ImVec2") || def.StartsWith("ImVec4") || def.StartsWith("ImPlotPoint")))
            {
                t += "?";
                def = "null";
            }
            arguments[i] = new(t, Name(f.argsT[i].name), def);
        }
        for (int i = 0; i < f.argsT.Length; i++)
        {
            if (f.defaults.TryGetValue(f.argsT[i].name, out var def))
                def = ConvertDefault(TypeHandling.ManagedType(argTypes[i]), def, genEnums, enumValues);
            callValues[i] = ConvertArg(TypeHandling.ManagedType(argTypes[i]), f.argsT[i].name, def);
        }

        return new ManagedFunction(f.funcname, returnType, arguments.ToArray(), f.ov_cimguiname, callValues.ToArray());
    }

    private static HashSet<string> refTypes =
    [
        "int*", "uint*", "long*", "ulong*", "short*", "ushort*",
        "float*", "double*", "sbyte*"
    ];

    static string PtrToRef(string ptr) => "ref " + ptr.Substring(0, ptr.Length - 1);
    static string RefToPtr(string r) => r.Substring(4) + "*";

    static List<ManagedFunction> GetFunctions(IEnumerable<FuncDefinition> allFunctions, HashSet<string> genEnums, Dictionary<string, string> enumValues)
    {
        List<ManagedFunction> converted = [];
        foreach (var f in allFunctions)
        {
            var c = ConvertFunction(f, genEnums, enumValues);

            // float*/double* to ref
            if (!c.Name.Contains("Colormap"))
            {
                for (int i = 0; i < c.Arguments.Length; i++)
                {
                    if (!refTypes.Contains(c.Arguments[i].Type) || c.Arguments[i].DefaultValue != null)
                        continue;
                    c.Arguments[i] = c.Arguments[i] with { Type = PtrToRef(c.Arguments[i].Type) };
                    c.CallValues[i] += "_ptr";
                }
            }

            if (c.Arguments.Length > 3 && c.Arguments[^3].Name.StartsWith("out_") &&
                c.Arguments[^3].DefaultValue == "null")
            {
                // With optional parameters
                var noPtrs = c.Arguments.Take(c.Arguments.Length - 3).ToArray();
                var noPtrsVals = c.CallValues.ToArray();
                noPtrsVals[^3] = noPtrsVals[^2] = noPtrsVals[^1] = "null";
                converted.Add(c with { Arguments = noPtrs, CallValues = noPtrsVals });
                // Up to first ref
                var noDefs = noPtrs.Select(x => x with { DefaultValue = null }).ToArray();
                var out1 = noDefs.Concat([
                    new ("out bool", c.Arguments[^3].Name, null)
                ]).ToArray();
                var out1Vals = c.CallValues.ToArray();
                out1Vals[^3] = $"&{c.Arguments[^3].Name}_byte";
                out1Vals[^2] = "null";
                out1Vals[^1] = "null";
                converted.Add(c with { Arguments = out1,  CallValues = out1Vals });
                //Up to second ref
                var out2 = noDefs.Concat([
                    new ("out bool", c.Arguments[^3].Name, null),
                    new ("out bool", c.Arguments[^2].Name, null)
                ]).ToArray();
                var out2Vals = c.CallValues.ToArray();
                out2Vals[^3] = $"&{c.Arguments[^3].Name}_byte";
                out2Vals[^2] = $"&{c.Arguments[^2].Name}_byte";
                out2Vals[^1] = "null";
                converted.Add(c with { Arguments = out2,  CallValues = out2Vals });
                //Up to last ref
                var out3 = noDefs.Concat([
                    new ("out bool", c.Arguments[^3].Name, null),
                    new ("out bool", c.Arguments[^2].Name, null),
                    new ("out bool", c.Arguments[^1].Name, null),
                ]).ToArray();
                var out3Vals = c.CallValues.ToArray();
                out3Vals[^3] = $"&{c.Arguments[^3].Name}_byte";
                out3Vals[^2] = $"&{c.Arguments[^2].Name}_byte";
                out3Vals[^1] = $"&{c.Arguments[^1].Name}_byte";
                converted.Add(c with { Arguments = out3,  CallValues = out3Vals });
            }
            else
            {
                converted.Add(c);
            }
        }
        return converted;
    }

    public static void Write(IEnumerable<FuncDefinition> allFunctions, HashSet<string> genEnums,
        Dictionary<string, string> enumValues)
    {
        var all = GetFunctions(allFunctions, genEnums, enumValues);

        using var cs = new StreamWriter("ImPlot.cs");

        cs.WriteLine(@"// <auto-generated/>
#nullable disable
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;

namespace LibreLancer.ImUI.ImPlot;

public static unsafe class ImPlot
{");
        foreach (var f in all)
        {
            cs.Write($"    public static {f.ReturnType} {f.Name}(");
            for (int i = 0; i < f.Arguments.Length; i++)
            {
                if(i > 0)
                    cs.Write(", ");
                cs.Write($"{f.Arguments[i].Type} {Name(f.Arguments[i].Name)}");
                if (f.Arguments[i].DefaultValue != null)
                {
                    cs.Write($" = {f.Arguments[i].DefaultValue}");
                }
            }
            cs.WriteLine(")");
            cs.WriteLine("    {");
            bool hasFixed = false;
            bool hasOut = false;
            foreach (var arg in f.Arguments)
            {
                if (arg.Type == "out bool")
                {
                    cs.WriteLine($"        byte {arg.Name}_byte = 0;");
                    hasOut = true;
                }
            }
            if(hasOut && f.ReturnType != "void")
                cs.WriteLine($"        {f.ReturnType} retval;");
            foreach (var arg in f.Arguments)
            {
                if (arg.Type == "string")
                {
                    cs.WriteLine(
                        $"        using var {arg.Name}_u8 = new UTF8ZHelper(stackalloc byte[128], {arg.Name} ?? \"\");");
                    hasFixed = true;
                }

                if (arg.Type == "ImPlotSpec?")
                {
                    cs.WriteLine($"        var {arg.Name}_spec = {arg.Name} ?? new ImPlotSpec();");
                }

                if (arg.Type == "string[]")
                {
                    cs.WriteLine($"        using var {arg.Name}_u8 = new NativeStringArray({arg.Name});");
                }
            }
            foreach (var arg in f.Arguments)
            {
                if (arg.Type == "string")
                {
                    cs.WriteLine($"        fixed(byte* {arg.Name}_ptr = {arg.Name}_u8.ToUTF8Z())");
                }
                if (arg.Type.StartsWith("ref "))
                {
                    cs.WriteLine($"        fixed({RefToPtr(arg.Type)} {arg.Name}_ptr = &{arg.Name})");
                    hasFixed = true;
                }
            }
            if(hasFixed)
                cs.Write("    "); //indent fixed
            var sb = new StringBuilder();
            sb.Append("ImPlotNative.").Append(f.NativeFunction).Append("(");
            for (int i = 0; i < f.CallValues.Length; i++)
            {
                if(i > 0)
                    sb.Append(", ");
                sb.Append($"{f.CallValues[i]}");
            }
            sb.Append(")");
            cs.WriteLine(Return(f.ReturnType, sb.ToString(), hasOut));
            if (hasOut)
            {
                foreach (var a in f.Arguments)
                {
                    if (a.Type == "out bool")
                    {
                        cs.WriteLine($"        {a.Name} = {a.Name}_byte != 0;");
                    }
                }
                if (f.ReturnType != "void")
                    cs.WriteLine("        return retval;");
            }
            cs.WriteLine("    }");
            cs.WriteLine();
        }
        cs.WriteLine("}");
    }
}

public record ManagedArgument(string Type, string Name, string? DefaultValue);

public record ManagedFunction(string Name, string ReturnType, ManagedArgument[] Arguments,
    string NativeFunction, string[] CallValues);
