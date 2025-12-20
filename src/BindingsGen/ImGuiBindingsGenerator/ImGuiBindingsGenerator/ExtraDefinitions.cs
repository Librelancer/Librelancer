namespace ImGuiBindingsGenerator;

public record ReplacementStruct(string Cpp, string Cs);
public record RedirectStruct(string Old, string New);
public record ExtraDefinitions(
    List<string> ByvalueStructs,
    List<string> RefStructs,
    List<string> Defines,
    List<ReplacementStruct> Replacements,
    List<RedirectStruct> Redirects,
    List<string> Skip,
    List<string> UnformattedHelpers,
    List<string> ManualWrappers);
