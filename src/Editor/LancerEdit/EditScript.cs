// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LibreLancer;
using LibreLancer.Ini;

namespace LancerEdit
{
    public enum ScriptArgumentType
    {
        String,
        File,
        SaveFile,
        Folder,
        Boolean,
        Flag,
        Integer,
        Dropdown,
        FileArray,
        FileFolderArray,
    }
    public class EditScriptArgument
    {
        [Entry("name", Required = true)] public string Name;
        [Entry("type", Required = true)] public ScriptArgumentType Type;
        [Entry("option", Multiline = true)] public List<string> Options = new List<string>();
        [Entry("flag")] public string Flag;
    }

    public class EditScript : IniFile
    {
        public class EditScriptSection
        {
            [Entry("name", Required = true)] public string Name;
        }

        [Section("script")] public EditScriptSection Info;
        [Section("argument")] public List<EditScriptArgument> Arguments = new List<EditScriptArgument>();

        public string Filename;

        public bool Validate()
        {
            foreach (var arg in Arguments) {
                if (arg.Type == ScriptArgumentType.Dropdown && arg.Options.Count == 0)
                {
                    FLLog.Error("Scripts", $"`{Filename}` argument `{arg.Name}` is type dropdown but is missing options");
                    return false;
                }
            }
            return true;
        }

        public EditScript(string filename)
        {
            Filename = filename;
            var frontMatter = GetFrontMatter(File.ReadAllText(filename));
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(frontMatter))) {
                ParseAndFill($"Frontmatter: {Path.GetFileName(filename)}", stream);
            }
        }

        public static string GetFrontMatter(string src)
        {
            var lines = src.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0) return string.Empty;
            int i = 0;
            if (lines[0].StartsWith("#!")) i++;
            var frontMatter = new StringBuilder();
            bool inMultiline = false;
            for (; i < lines.Length; i++)
            {
                if (inMultiline)
                {
                    int startIdx = 0;
                    if (lines[i][0] == '*' && lines[i].Length > 1 && lines[i][1] != '/')
                        startIdx++;
                    var idx = lines[i].IndexOf("*/");
                    if (idx != -1) {
                        if (idx != lines[i].Length - 2)
                        {
                            frontMatter.AppendLine(lines[i].Substring(startIdx, idx - startIdx).Trim());
                            break;
                        }
                        else {
                            inMultiline = false;
                            frontMatter.AppendLine(lines[i].Substring(startIdx, idx - startIdx).Trim());
                        }
                    } else {
                        frontMatter.AppendLine(lines[i].Substring(startIdx).Trim());
                    }
                }
                else if (lines[i].StartsWith("//"))
                {
                    frontMatter.AppendLine(lines[i].Substring(2).Trim());
                }
                else if (lines[i].StartsWith("/*"))
                {
                    var startIdx = 2;
                    var idx = lines[i].IndexOf("*/");
                    if (idx == -1)
                    {
                        inMultiline = true;
                        frontMatter.AppendLine(lines[i].Substring(2).Trim());
                    } else {
                        frontMatter.AppendLine(lines[i].Substring(2, idx - 2).Trim());
                        if (idx != lines[i].Length - 2)
                            break;
                    }
                } else {
                    break;
                }
            }
            return frontMatter.ToString();
        }
    }
}
