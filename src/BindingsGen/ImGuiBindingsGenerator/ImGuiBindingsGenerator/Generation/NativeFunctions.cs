namespace ImGuiBindingsGenerator.Generation;

public static class NativeFunctions
{
    public static void Write(ProcessedDefinitions defs,TypeConversions types, string outputDir)
    {
        var tw = new CodeWriter();
        tw.AppendLine("using System;");
        tw.AppendLine("using System.Runtime.InteropServices;");
        tw.AppendLine();
        tw.AppendLine("namespace ImGuiNET;");
        tw.AppendLine();
        tw.AppendLine("public static unsafe class ImGuiNative");
        using (tw.Block())
        {
            foreach (var pf in defs.Functions)
            {
                var f = pf.Function;
                tw.AppendComments(f.Comments);
                if (!string.IsNullOrWhiteSpace(pf.EntrypointName))
                {
                    tw.AppendLine($"[DllImport(\"cimgui\", EntryPoint=\"{pf.EntrypointName}\")]");

                }
                else
                {
                    tw.AppendLine("[DllImport(\"cimgui\")]");
                }
                tw.Append("public static extern ");
                var context = string.IsNullOrWhiteSpace(f.OriginalClass)
                    ? f.Name
                    : $"{f.OriginalClass}_{f.Name}";
                var returnType = types.GetConversion(context, f.ReturnType!);
                tw.Append(returnType.InteropName).Append(" ");
                tw.Append(f.Name);
                tw.Append("(");
                var saneArgs = f.Arguments.Where(x => !x.IsVarargs).ToArray();
                for (int i = 0; i < saneArgs.Length; i++)
                {
                    var argContext = $"{context}_{saneArgs[i].Name}";
                    var argType = types.GetConversion(argContext, saneArgs[i].Type!);
                    tw.Append(argType.InteropName)
                        .Append(" ")
                        .Append(ItemUtilities.FixIdentifier(saneArgs[i].Name));
                    if (i + 1 < saneArgs.Length)
                    {
                        tw.Append(", ");
                    }
                }
                tw.AppendLine(");").AppendLine();
            }
        }

        File.WriteAllText(Path.Combine(outputDir, $"ImGuiNative.cs"), tw.ToString());
    }
}
