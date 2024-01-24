using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BuildLL;
using static BuildLL.Runtime;

public static class MingwDeps
{
    public static void CopyMingwDependencies(string prefix, string targetfolder)
    {
        var deps = new HashSet<string>();
        for (int i = 0; i < 2; i++) //Search twice
        {
            foreach (var f in Directory.GetFiles(targetfolder, "*.dll"))
            {
                var dependencies = GetDllsForFile(prefix, f);
                foreach (var d in dependencies)
                    if (!deps.Contains(d))
                        deps.Add(d);
            }
            foreach (var d in deps)
            {
                var f = FindFile(prefix, d);
                if (f != null)
                {
                    Console.WriteLine($"Copying dependency: {f}");
                    CopyFile(f, Path.Combine(targetfolder, d));
                }
            }
        }
    }
    static string FindFile(string prefix, string file)
    {
        var f = Directory.GetFiles($"/usr/{prefix}", file, SearchOption.AllDirectories).FirstOrDefault();
        if(f == null && Directory.Exists($"/usr/lib/gcc/{prefix}")) {
            var posixDir = Directory.GetDirectories($"/usr/lib/gcc/{prefix}").FirstOrDefault(x => x.EndsWith("posix"));
            if(posixDir != null)
                f = Directory.GetFiles(posixDir, file, SearchOption.AllDirectories).FirstOrDefault();
        }
        return f;
    }
    static string[] GetDllsForFile(string prefix, string file)
    {
        return Bash(
                $"{prefix}-objdump -p {Quote(file)} | grep 'DLL Name:' | sed -e \"s/\t*DLL Name: //g\" | grep '^lib' | cat")
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }
}
