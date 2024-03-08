namespace LibreLancer.Ini;

public static class IniWarning
{
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
    
    static string FormatLine(string file, int line)
    {
        if (line >= 0)
            return $" at {file}, line {line}";
        else
            return $" in {file} (line not available)";
    }

    static string FormatLine(string file, int line, Section? section)
    {
        if (section == null)
            return FormatLine(file, line);
        if (line >= 0)
            return $" at section {section}: {file}, line {line}";
        else
            return $" in section {section}: {file} (line not available)";
    }
}
