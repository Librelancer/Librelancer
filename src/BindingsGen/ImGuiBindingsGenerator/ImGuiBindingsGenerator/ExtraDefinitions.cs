namespace ImGuiBindingsGenerator;

public record ReplacementStruct(string cpp, string cs);
public record ExtraDefinitions(
    List<string> StructClasses, 
    List<string> Defines, 
    List<ReplacementStruct> Replacements,
    List<string> UnformattedHelpers,
    List<string> ManualWrappers);