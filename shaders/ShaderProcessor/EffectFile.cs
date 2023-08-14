// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ShaderProcessor;

public class EffectFile
{
    private const string RInclude = @"^\s*@\s*include\s*\(([^\)]*)\)\s*";

    private static readonly Regex IncludeRegex = new(RInclude,
        RegexOptions.ECMAScript | RegexOptions.Compiled | RegexOptions.Multiline);

    public string[] Features;
    public string FragmentSource;
    public string Name;
    public string VertexSource;
    public string GeometrySource;

    private static string ProcessIncludes(string fname, string src, string directory)
    {
        var m = IncludeRegex.Match(src);
        var newsrc = src;
        while (m.Success)
        {
            var infile = Path.Combine(directory, m.Groups[1].Value);
            if (!File.Exists(infile))
            {
                Console.Error.WriteLine($"{fname}: Could not find include '{infile}'");
                return null;
            }

            var newdir = Path.GetDirectoryName(infile);
            var inc = ProcessIncludes(Path.GetFileName(infile), File.ReadAllText(infile), newdir);
            newsrc = newsrc.Remove(m.Index, m.Length).Insert(m.Index, inc);
            m = IncludeRegex.Match(newsrc);
        }

        return newsrc;
    }

    private static string SaneName(string name)
    {
        name = name.Trim();
        var builder = new StringBuilder();
        if (char.IsDigit(name[0])) builder.Append("_");
        for (var i = 0; i < name.Length; i++)
            if (char.IsWhiteSpace(name[i]))
                builder.Append("_");
            else if (char.IsSymbol(name[i]))
                builder.Append("_");
            else
                builder.Append(name[i]);
        return builder.ToString();
    }

    public static EffectFile Read(string filename)
    {
        filename = Path.GetFullPath(filename);
        var txt = File.ReadAllText(filename);
        txt = ProcessIncludes(Path.GetFileName(filename), txt, Path.GetDirectoryName(filename));
        if (txt == null) return null;
        var effectFile = new EffectFile();
        var features = new List<string>();
        using (var reader = new StringReader(txt))
        {
            var inMultilineComment = false;
            StringBuilder currentBlock = null;
            string currentBlockName = null;
            int blockType = -1;
            var lineNumber = 1;
            string ln;
            while ((ln = reader.ReadLine()) != null)
            {
                var mlEnd = -1;
                if (!inMultilineComment)
                {
                    var mlStart = ln.IndexOf("/*", StringComparison.Ordinal);
                    if (mlStart != -1)
                    {
                        mlEnd = ln.IndexOf("*/", StringComparison.Ordinal);
                        if (mlEnd == -1) inMultilineComment = true;
                    }
                }
                else
                {
                    mlEnd = ln.IndexOf("*/", StringComparison.Ordinal);
                    if (mlEnd != -1) inMultilineComment = false;
                }

                var idx = ln.IndexOf('@');
                if (idx != -1 && !inMultilineComment && mlEnd < 0)
                {
                    var valid = true;
                    for (var i = 0; i < idx; i++)
                        if (!char.IsWhiteSpace(ln[i]))
                        {
                            valid = false;
                            break;
                        }

                    if (valid)
                    {
                        var directive = ln.Substring(idx + 1).Trim();
                        directive = directive.Replace("\t", " ");
                        var vals = directive.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (vals.Length > 0)
                        {
                            switch (vals[0].ToLowerInvariant())
                            {
                                case "name":
                                case "vertex":
                                case "fragment":
                                case "feature":
                                case "geometry":
                                case "lazy":
                                    if (currentBlock != null)
                                    {
                                        if (blockType == 2) effectFile.FragmentSource = currentBlock.ToString();
                                        else if (blockType == 1) effectFile.VertexSource = currentBlock.ToString();
                                        else if (blockType == 0) effectFile.GeometrySource = currentBlock.ToString();
                                    }
                                    currentBlock = null;
                                    blockType = -1;
                                    break;
                            }

                            switch (vals[0].ToLowerInvariant())
                            {
                                case "name":
                                    effectFile.Name = directive.Substring(directive.IndexOf("name") + 4).Trim();
                                    break;
                                case "vertex":
                                    if (effectFile.VertexSource != null)
                                    {
                                        Console.Error.WriteLine($"Duplicate vertex block at {lineNumber}");
                                        return null;
                                    }

                                    currentBlock = new StringBuilder();
                                    blockType = 1;
                                    break;
                                case "geometry":
                                    if (effectFile.GeometrySource != null)
                                    {
                                        Console.Error.WriteLine($"Duplicate geometry block at {lineNumber}");
                                        return null;
                                    }

                                    currentBlock = new StringBuilder();
                                    blockType = 0;
                                    break;
                                case "fragment":
                                    if (effectFile.FragmentSource != null)
                                    {
                                        Console.Error.WriteLine($"Duplicate fragment block at {lineNumber}");
                                        return null;
                                    }

                                    currentBlock = new StringBuilder();
                                    blockType = 2;
                                    break;
                                case "feature":
                                    features.Add(vals[1]);
                                    break;
                                default:
                                    Console.Error.WriteLine($"Invalid directive {ln} at {lineNumber}");
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    if (currentBlock != null) currentBlock.AppendLine(ln);
                }

                lineNumber++;
            }

            if (currentBlock != null)
            {
                if (blockType == 2) effectFile.FragmentSource = currentBlock.ToString();
                else if (blockType == 1) effectFile.VertexSource = currentBlock.ToString();
                else if (blockType == 0) effectFile.GeometrySource = currentBlock.ToString();   
            }
        }

        if (string.IsNullOrWhiteSpace(effectFile.Name))
            effectFile.Name = Path.GetFileNameWithoutExtension(filename);
        effectFile.Name = SaneName(effectFile.Name);
        effectFile.Features = features.ToArray();
        if (effectFile.VertexSource == null)
        {
            Console.Error.WriteLine("Vertex source not specified");
            return null;
        }

        if (effectFile.FragmentSource == null)
        {
            Console.Error.WriteLine("Fragment source not specified");
            return null;
        }

        return effectFile;
    }
}