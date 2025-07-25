namespace ImGuiBindingsGenerator;

public record ReplacementStruct(string cpp, string cs);
public record ExtraDefinitions(
    List<string> ByvalueStructs,
    List<string> RefStructs,
    List<string> Defines,
    List<ReplacementStruct> Replacements,
    List<string> UnformattedHelpers,
    List<string> ManualWrappers);
