using System.Text;
using System.Text.RegularExpressions;

namespace ImGuiBindingsGenerator.Generation;

public static class FunctionWrappers
{
    private static Regex Vec2Regex = new Regex(@"ImVec2\((.*), (.*)\)");
    private static Regex Vec4Regex = new Regex(@"ImVec4\((.*), (.*), (.*), (.*)\)");

    static string ConstantResolve(string value)
    {
        return value.Replace("FLT_MIN", "float.Epsilon")
            .Replace("FLT_MAX", "float.MaxValue");
    }

    static string DefaultValue(string value, bool isIntPtr, out bool needOptional)
    {
        needOptional = false;
        if (value.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            return isIntPtr ? "0" : "null";
        }
        if (value == "ImVec2(0, 0)")
            return "default";
        if (value == "ImVec4(0, 0, 0, 0)")
            return "default";
        if (value == "IM_COL32_WHITE")
            return "0xFFFFFFFFU";
        var vm = Vec2Regex.Match(value);
        if (vm.Success)
        {
            var x = ConstantResolve(vm.Groups[1].Value);
            var y = ConstantResolve(vm.Groups[2].Value);
            needOptional = true;
            value = $"new System.Numerics.Vector2({x}, {y})";
        }

        vm = Vec4Regex.Match(value);
        if (vm.Success)
        {
            var x = ConstantResolve(vm.Groups[1].Value);
            var y = ConstantResolve(vm.Groups[2].Value);
            var z = ConstantResolve(vm.Groups[3].Value);
            var w = ConstantResolve(vm.Groups[4].Value);
            needOptional = true;
            value = $"new System.Numerics.Vector4({x}, {y}, {z}, {w})";
        }

        return ConstantResolve(value);
    }

    record ResolvedArg(FunctionArgument Arg, TypeConversion Type, bool Optional, string? DefaultValue, string? Literal);

    record Overload(ResolvedArg[] args);

    static List<Overload> GetOverloads(string context, int argsStart, FunctionItem f, TypeConversions types)
    {
        List<Overload> overloads = new List<Overload>();
        var allArgs = f.Arguments
            .Skip(argsStart)
            .Where(x => !x.IsVarargs).Select(x =>
            {
                var argContext = $"{context}_{x.Name}";
                bool optional = false;
                string? defaultValue = null;
                var conv = types.GetConversion(argContext, x.Type!);
                if (conv is FixedArrayType arrayType)
                {
                    conv = arrayType.AsRefParameter();
                }
                if (!string.IsNullOrWhiteSpace(x.DefaultValue))
                {
                    defaultValue = DefaultValue(x.DefaultValue, conv.FriendlyName == "IntPtr", out optional);
                    if (conv.Kind == TypeKind.Enum)
                        defaultValue = $"({conv.FriendlyName}){defaultValue}";
                }

                // Convert text_end to something we can use
                if (conv.Kind == TypeKind.String &&
                    x.Name == "text_end")
                {
                    if (f.Arguments.Skip(argsStart).Any(y => y.Name == "text"))
                    {
                        return new ResolvedArg(x, new TextEnd("text"), optional, defaultValue, null);
                    }
                    if (f.Arguments.Skip(argsStart).Any(y => y.Name == "text_begin"))
                    {
                        return new ResolvedArg(x, new TextEnd("text_begin"), optional, defaultValue, null);
                    }
                }
                // Regular argument
                return new ResolvedArg(x, conv, optional, defaultValue, null);
            }).ToArray();

        void AddOverload(int count, bool clearDefaults)
        {
            var newArgs = allArgs.Take(count)
                .Select(x => clearDefaults ? x with { DefaultValue = null } : x);
            var literalArgs = allArgs.Skip(count)
                .Select(x => x with { Literal = x.DefaultValue });
            overloads.Add(new(newArgs.Concat(literalArgs).ToArray()));
        }
        
        int generateOverloads = -1;
        for (int i = 0; i < allArgs.Length; i++)
        {
            if (allArgs[i].Type.CppName != "void*" &&
                allArgs[i].Type.CppName != "void**" &&
                allArgs[i].Type.Kind == TypeKind.Pointer &&
                allArgs[i].DefaultValue != null)
            {
                generateOverloads = i;
                break;
            }
        }

        if (generateOverloads == -1)
        {
            AddOverload(allArgs.Length, false);
        }
        else
        {
            AddOverload(generateOverloads, false);
            for (int i = generateOverloads + 1; i <= allArgs.Length; i++)
            {
                AddOverload(i, true);
            }
        }
        return overloads;
    }


    public static void WrapFunction(CodeWriter tw, ProcessedFunction pf, TypeConversions types, bool isByValueStruct = false)
    {
        if (pf.SkipWrapping)
            return;
        
        FunctionItem f = pf.Function;
        
        string functionPrefix = string.IsNullOrWhiteSpace(f.OriginalClass)
            ? "ImGui_"
            : $"{f.OriginalClass}_";
        int instanceMethod = string.IsNullOrWhiteSpace(f.OriginalClass) ? 0 : 1;
        var context = string.IsNullOrWhiteSpace(f.OriginalClass)
            ? f.Name
            : $"{f.OriginalClass}_{f.Name}";
        var returnType = types.GetConversion(context, f.ReturnType!);

        if (instanceMethod == 1)
        {
            // Method is in a class, but could still be static
            if (f.Arguments is not { Count: > 0 } ||
                !f.Arguments[0].IsInstancePointer)
            {
                instanceMethod = 0;
            }
        }
        

        var overloads = GetOverloads(context, instanceMethod, f, types);
        foreach (var o in overloads)
        {
            var allArgs = o.args;
            tw.AppendComments(f.Comments);
            tw.Append("public ");
            if (instanceMethod == 0)
                tw.Append("static ");
            tw.Append(returnType.FriendlyName).Append(" ");
            tw.Append((pf.RemappedName ?? f.Name).Replace(functionPrefix, ""));
            tw.Append("(");
            var sigArgs = o.args.Where(x => x.Literal == null).ToArray();
            for (int i = 0; i < sigArgs.Length; i++)
            {
                var fName = sigArgs[i].Type.FriendlyName;
                tw.Append(sigArgs[i].Optional ? $"ImOptionalArg<{fName}>" : fName);
                tw.Append(" ");
                tw.Append(ItemUtilities.FixIdentifier(sigArgs[i].Arg.Name));
                if (!string.IsNullOrWhiteSpace(sigArgs[i].DefaultValue))
                {
                    tw.Append(" = ");
                    tw.Append(sigArgs[i].Optional ? "default" : sigArgs[i].DefaultValue!);
                }

                if (i + 1 < sigArgs.Length)
                {
                    tw.Append(", ");
                }
            }

            tw.AppendLine(")");
            using (tw.Block())
            {
                if (isByValueStruct && instanceMethod == 1)
                {
                    tw.AppendLine($"fixed({f.OriginalClass}* __self_ptr = &this)");
                    tw.AppendLine("{").Indent();
                }
                bool finallyBlock = false;
                foreach (var arg in sigArgs)
                {
                    var argIdent = ItemUtilities.FixIdentifier(arg.Arg.Name);
                    if (arg.Type.ParameterConversionStart(tw, argIdent))
                        finallyBlock = true;
                }

                if (finallyBlock)
                {
                    tw.AppendLine("try");
                    tw.AppendLine("{").Indent();
                }
                var sb = new StringBuilder();
                sb.Append("ImGuiNative.").Append(f.Name).Append("(");
                if (instanceMethod == 1)
                {
                    sb.Append(isByValueStruct ? "__self_ptr" : "this.Handle");
                    if (allArgs.Length > 0)
                        sb.Append(", ");
                }

                for (int i = 0; i < allArgs.Length; i++)
                {
                    if (allArgs[i].Literal != null)
                    {
                        sb.Append(allArgs[i].Literal);
                    }
                    else
                    {
                        var argIdent = ItemUtilities.FixIdentifier(allArgs[i].Arg.Name);
                        sb.Append(allArgs[i].Type
                            .GetToInterop(allArgs[i].Optional
                                ? $"{argIdent}.Get({allArgs[i].DefaultValue})"
                                : argIdent));
                    }
                    if (i + 1 < allArgs.Length)
                    {
                        sb.Append(", ");
                    }
                }

                sb.Append(")");
                if (returnType.Kind == TypeKind.Void)
                    tw.Append(sb.ToString()).AppendLine(";");
                else
                    tw.Append("return ").Append(returnType.GetToFriendly(sb.ToString())).AppendLine(";");
                if (finallyBlock)
                {
                    tw.UnIndent().AppendLine("}");
                    tw.AppendLine("finally");
                    using (tw.Block())
                    {
                        foreach (var arg in sigArgs)
                        {
                            var argIdent = ItemUtilities.FixIdentifier(arg.Arg.Name);
                            arg.Type.ParameterConversionFinally(tw, argIdent);
                        }
                    }
                }
                foreach (var arg in sigArgs)
                {
                    var argIdent = ItemUtilities.FixIdentifier(arg.Arg.Name);
                    arg.Type.ParameterConversionEnd(tw, argIdent);
                }

                if (isByValueStruct && instanceMethod == 1)
                {
                    tw.UnIndent().AppendLine("}");
                }
            }
        }
    }

    public static void WriteImGui(ProcessedDefinitions defs, TypeConversions types, string outputDir)
    {
        var tw = new CodeWriter();
        tw.AppendLine("using System;");
        tw.AppendLine("using System.Runtime.CompilerServices;");
        tw.AppendLine("using System.Runtime.InteropServices;");
        tw.AppendLine();
        tw.AppendLine("namespace ImGuiNET;");
        tw.AppendLine();
        tw.AppendLine("public static unsafe partial class ImGui");
        using (tw.Block())
        {
            foreach (var f in defs.Functions)
            {
                if (!string.IsNullOrWhiteSpace(f.Function.OriginalClass))
                    continue;
                WrapFunction(tw, f, types);
            }
        }

        File.WriteAllText(Path.Combine(outputDir, $"ImGui.cs"), tw.ToString());
    }
}