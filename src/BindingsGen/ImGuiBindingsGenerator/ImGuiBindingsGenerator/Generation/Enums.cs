namespace ImGuiBindingsGenerator.Generation;

public static class Enums
{
    public static void WriteEnum(EnumItem cppEnum, ProcessedDefinitions definitions, TypeConversions types, string outputDir)
    {
        // Transform names
        var newName = cppEnum.Name.TrimEnd('_');
        var replaceName = cppEnum.Name.EndsWith("_")
            ? cppEnum.Name
            : cppEnum.Name + "_";
    
        string baseType = "int";
        var typeDef = definitions.Typedefs.FirstOrDefault(x => x.Name == newName || x.Name == cppEnum.Name);
        if (typeDef != null && typeDef.Type.Description.Kind == "Builtin")
        {
            baseType = TypeConversions.GetEnumBaseType(typeDef.Type.Description.BuiltinType!);
        }
        types.RegisterEnum(cppEnum.Name, newName);
        types.RegisterEnum(newName, newName);
        var tw = new CodeWriter();
        if (cppEnum.IsFlagsEnum)
        {
            tw.AppendLine("using System;").AppendLine();
        }
        tw.AppendLine("namespace ImGuiNET;");
        tw.AppendLine();
        tw.AppendComments(cppEnum.Comments);
        if (cppEnum.IsFlagsEnum)
        {
            tw.AppendLine("[Flags]");
        }
        tw.Append($"public enum {newName}");
        if (baseType != "int")
        {
            tw.Append($" : {baseType}");
        }
        tw.AppendLine();
        using (tw.Block())
        {
            for (int i = 0; i < cppEnum.Elements.Count; i++)
            {
                var item = cppEnum.Elements[i];
                tw.AppendComments(item.Comments);
                var elementName = ItemUtilities.FixIdentifier(item.Name.Replace(replaceName, ""));
                var value = string.IsNullOrWhiteSpace(item.ValueExpression)
                    ? item.Value.ToString()
                    : item.ValueExpression.Replace(replaceName, "");
                tw.Append($"{elementName} = {value}");
                if (i + 1 < cppEnum.Elements.Count)
                    tw.AppendLine(",");
                else
                    tw.AppendLine();
                definitions.Defines.AddConstant(item.Name, $"((int)({newName}.{elementName}))");
            }
        }
        File.WriteAllText(Path.Combine(outputDir, "Enums", newName + ".cs"), tw.ToString());
    }
}