using System;
using System.Collections.Generic;
using System.IO;

namespace BuildLL;

public class ConfigFile
{
    private Dictionary<string, string> configKeys = new(StringComparer.OrdinalIgnoreCase);
    public ConfigFile() { }
    public ConfigFile(string path)
    {
        var lines = File.ReadAllLines(path);
        foreach (var ogln in lines)
        {
            var comment = ogln.IndexOf('#');
            var ln = comment == -1 ? ogln.Trim() : ogln.Substring(0, comment).Trim();
            if (string.IsNullOrWhiteSpace(ln)) continue;
            var ws = ln.IndexOf('=');
            if (ws == -1)
                continue;
            var key = ln.Substring(0, ws).Trim();
            var value = ln.Substring(ws + 1).Trim();
            configKeys[key] = value;
        }
    }

    public bool GetBool(string key)
    {
        var v = this[key];
        if(string.IsNullOrWhiteSpace(v))
            return false;
        return v.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               v.Equals("1", StringComparison.OrdinalIgnoreCase);
    }

    public string this[string key]
    {
        get
        {
            if (configKeys.TryGetValue(key, out var value))
                return value;
            return "";
        }
    }
}
