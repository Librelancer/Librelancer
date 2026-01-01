using System;
using System.Collections.Generic;
using System.Linq;

namespace LibreLancer.Data.Ini;

public static class ParseHelpers
{
    public static uint Hash(string s)
    {
        uint num = 0x811c9dc5;

        for (int i = 0; i < s.Length; i++)
        {
            var c = (int) s[i];
            if ((c >= 65 && c <= 90))
                c ^= (1 << 5);
            num = ((uint) c ^ num) * 0x1000193;
        }

        return num;
    }

    public static bool ComponentCheck(int c, Section s, Entry e, int min = -1)
    {
        if (min == -1) min = c;
        if (e.Count > c)
            FLLog.Warning("Ini", "Too many components for " + e.Name + IniDiagnostic.FormatLine(s.File, e.Line, s));
        if (e.Count >= min)
            return true;
        FLLog.Error("Ini", "Not enough components for " + e.Name + IniDiagnostic.FormatLine(s.File, e.Line, s));
        return false;
    }

    private static bool HasIgnoreCase(string[] array, string value) => array.Any(t => t.Equals(value, StringComparison.OrdinalIgnoreCase));

    public static IEnumerable<Section> Chunk(string[] delimiters, Section parent)
    {
        Section? currentSection = null;

        foreach (var e in parent)
        {
            if (HasIgnoreCase(delimiters, e.Name))
            {
                if (currentSection != null)
                    yield return currentSection;
                currentSection = new Section(parent.Name)
                {
                    File = parent.File,
                    Line = parent.Line
                };
            }

            if (currentSection != null)
                currentSection.Add(e);
            else
                IniDiagnostic.EntryWithoutObject(e, parent);
        }

        if (currentSection != null) yield return currentSection;
    }
}
