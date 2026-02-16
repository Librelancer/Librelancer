namespace LibreLancer.Data.Ini;

public static class IniDiagnostic
{
    public static void ChildAddFailure(Section section)
    {
        FLLog.Error("Ini", $"Could not add section [{section.Name}] to parent{FormatLine(section.File, section.Line)}");
    }
    public static void MissingField(string field, Section section)
    {
        FLLog.Error("Ini",
            $"Missing required field {field}{FormatLine(section.File, section.Line, section)}");
    }

    public static void InvalidEnum(Entry e, Section s)
    {
        FLLog.Error("Ini", "Invalid value for enum " + e[0].ToString() + FormatLine(s.File, e.Line, s));
    }

    public static void InvalidGuid(Entry e, Section s)
    {
        FLLog.Error("Ini", "Invalid value for guid " + e[0].ToString() + FormatLine(s.File, e.Line, s));
    }

    public static void UnknownEntry(Entry e, Section s)
    {
        FLLog.Warning("Ini", "Unknown entry " + e.Name + FormatLine(s.File, e.Line, s));
    }

    public static void UnknownSection(Section section)
    {
        FLLog.Warning("Ini", "Unknown section " + section.Name + FormatLine(section.File, section.Line));
    }

    public static void DuplicateEntry(Entry e, Section s)
    {
        FLLog.Warning("Ini", "Duplicate of " + e.Name + FormatLine(s.File, e.Line, s));
    }

    public static void Warn(string warning, Entry e)
    {
        FLLog.Warning("Ini", warning + " " + FormatLine(e.Section.File, e.Line, e.Section));
    }

    private static string FormatLine(string? file, int line)
    {
        if (line >= 0)
            return $" at {file}, line {line}";
        else
            return $" in {file} (line not available)";
    }

    public static void EntryWithoutObject(Entry e, Section s)
    {
        FLLog.Warning("Ini", $"Entry without object '{e.Name}' {FormatLine(s.File, e.Line, s)}");
    }


    public static string FormatLine(string? file, int line, Section? section)
    {
        if (section == null)
            return FormatLine(file, line);
        if (line >= 0)
            return $" at section {section}: {file}, line {line}";
        else
            return $" in section {section}: {file} (line not available)";
    }
}
