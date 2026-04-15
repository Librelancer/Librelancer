using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;
using LibreLancer.Data.Schema.Voices;

namespace LibreLancer.Data.Schema.GCS;

public record struct GenericScriptKey(string Segment, Posture Posture, FLGender Gender);

public class GenericScriptsIni
{
    public Dictionary<GenericScriptKey, List<string>> Scripts = new();

    public void AddFile(string path, FileSystem vfs, IniStringPool? stringPool = null)
    {
        foreach (var section in IniFile.ParseFile(path, vfs, false, stringPool))
        {
            if (!section.Name.Equals("genericscripts", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Unexpected section in genericscripts " + section);

            Posture? posture = null;
            FLGender? gender = null;
            string? segment = null;

            foreach (var e in section)
            {
                if (e.Count != 1)
                {
                    IniDiagnostic.Warn("Invalid number of values (must be 1)", e);
                    continue;
                }
                switch (e.Name.ToLowerInvariant())
                {
                    case "set_posture":
                        if (Enum.TryParse<Posture>(e[0].ToString(), true, out var p))
                        {
                            posture = p;
                        }
                        else
                        {
                            IniDiagnostic.InvalidEnum(e, section);
                        }
                        break;
                    case "set_gender":
                        if (Enum.TryParse<FLGender>(e[0].ToString(), true, out var g))
                        {
                            gender = g;
                        }
                        else
                        {
                            IniDiagnostic.InvalidEnum(e, section);
                        }
                        break;
                    case "set_segment":
                        segment = e[0].ToString();
                        break;
                    case "script":
                        if (posture == null ||
                            gender == null ||
                            string.IsNullOrEmpty(segment))
                        {
                            IniDiagnostic.EntryWithoutObject(e, section);
                        }
                        else
                        {
                            var k = new GenericScriptKey(segment, posture.Value, gender.Value);
                            if (!Scripts.TryGetValue(k, out var scripts))
                            {
                                scripts = new List<string>();
                                Scripts[k] = scripts;
                            }
                            scripts.Add(e[0].ToString());
                        }
                        break;
                }
            }
        }
    }
}
