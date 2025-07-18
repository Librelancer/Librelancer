using System.Text;

namespace ImGuiBindingsGenerator.Generation;

public class Delegates
{
    private Dictionary<string, string> generatedDelegates = new();

    private CodeWriter delWriter = new();

    public Delegates()
    {
        delWriter.AppendLine("using System;");
        delWriter.AppendLine("using System.Runtime.InteropServices;");
        
        delWriter.AppendLine("namespace ImGuiNET;");
        delWriter.AppendLine();
    }

    public string GenerateDelegate(string typeName, TypeConversions types, TypeDescription func)
    {
        if (generatedDelegates.TryGetValue(typeName, out var pointerType))
            return pointerType!;
        var ptrBuilder = new StringBuilder();
        ptrBuilder.Append("delegate* unmanaged<");
        delWriter.AppendLine("[UnmanagedFunctionPointer(CallingConvention.Cdecl)]");
        var returnType = types.GetConversion("", func.ReturnType!.Name ?? "", func.ReturnType!);
        delWriter.Append($"public unsafe delegate {returnType.InteropName} {typeName}(");
        if (func.Parameters != null)
        {
            for (int i = 0; i < func.Parameters.Count; i++)
            {
                var pName = ItemUtilities.FixIdentifier(func.Parameters[i].Name!);
                var paramType = types.GetConversion($"{typeName}_{pName}",
                    func.Parameters[i].InnerType!.Name ?? "", func.Parameters[i].InnerType!);
                delWriter.Append($"{paramType.InteropName} {pName}");
                if(i + 1 < func.Parameters.Count)
                    delWriter.Append(", ");
                ptrBuilder.Append(paramType.InteropName).Append(", ");
            }
        }
        delWriter.AppendLine(");");
        ptrBuilder.Append(returnType.InteropName).Append(">");
        generatedDelegates[typeName] = pointerType = ptrBuilder.ToString();
        return pointerType;
    }


    public void WriteFile(string outputDir)
    {
        File.WriteAllText(Path.Combine(outputDir, "ImGui.Delegates.cs"), delWriter.ToString());
    }
}