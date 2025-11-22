using System;
using System.Linq;
using System.Text;
using LibreLancer.GeneratorCommon;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace LibreLancer.Data.Generator;

public class IniParser
{
    static string ToLiteral(string input)
    {
        return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(input))
            .ToFullString();
    }

    public static void GenerateIniParser(SourceProductionContext context, ParsedIniInfo ini)
    {
        var tw = new TabbedWriter();
        tw.AppendLine("using System;");
        tw.AppendLine("using System.Collections.Generic;");
        tw.AppendLine("using System.Linq;");
        tw.AppendLine("using LibreLancer.Data.Ini;");
        tw.AppendLine("using SysIO = System.IO;");
        tw.AppendLine();
        tw.AppendLine("#nullable enable");
        tw.AppendLine();
        tw.AppendLine($"namespace {ini.Namespace};");
        tw.AppendLine();
        tw.AppendLine($"partial class {ini.Name}");
        tw.AppendLine("{").Indent();
        // Generate public-facing overloads
        tw.AppendLine("public void ParseInis(IEnumerable<string> files, LibreLancer.Data.IO.FileSystem? vfs, IniStringPool? stringPool = null, IniParseProperties? properties = null)");
        using (tw.Block())
        {
            tw.AppendLine(
                "_ParseInis(files.Select(x => (x, vfs == null ? SysIO.File.OpenRead(x) : vfs.Open(x))), true, stringPool, properties);");
        }

        tw.AppendLine();
        tw.AppendLine("public void ParseIni(string file, LibreLancer.Data.IO.FileSystem? vfs, IniStringPool? stringPool = null, IniParseProperties? properties = null)");
        using (tw.Block())
        {
            tw.AppendLine("_ParseInis([(file, vfs == null ? SysIO.File.OpenRead(file) : vfs.Open(file))], true, stringPool, properties);");
        }

        tw.AppendLine();
        tw.AppendLine("public void ParseIni(SysIO.Stream stream, string path, IniStringPool? stringPool = null, IniParseProperties? properties = null)");
        using (tw.Block())
        {
            tw.AppendLine("_ParseInis([(path, stream)], false, stringPool, properties);");
        }

        tw.AppendLine();
        // Actual parser
        tw.AppendEditorHiddenLine()
            .AppendLine("void _ParseInis(IEnumerable<(string Path, SysIO.Stream Stream)> files, bool closeStreams, IniStringPool? stringPool, IniParseProperties? properties)");
        using (tw.Block())
        {
            tw.AppendLine("foreach(var src in files)");
            using (tw.Block())
            {
                tw.AppendLine($"foreach(var section in LibreLancer.Data.Ini.IniFile.ParseFile(src.Path, src.Stream, {(ini.Preparse ? "true" : "false")}, false, stringPool))");
                using (tw.Block())
                {
                    tw.AppendLine("var hash = ParseHelpers.Hash(section.Name);");
                    tw.AppendLine("switch (hash)");
                    using (tw.Block())
                    {
                        foreach (var ignore in ini.IgnoreSections)
                        {
                            tw.AppendLine($"case 0x{IniHash.Hash(ignore):X}: break; //ignore");
                        }

                        foreach (var section in ini.Sections)
                        {
                            tw.AppendLine($"case 0x{IniHash.Hash(section.SectionName):X}:");
                            tw.AppendLine(
                                $"if({ToLiteral(section.SectionName)}.Equals(section.Name, StringComparison.OrdinalIgnoreCase))");
                            tw.AppendLine("{").Indent();
                            string toParse = "section";
                            //Splitting a section internally
                            if (section.Delimiters.Count > 0)
                            {
                                var delims = $"[{string.Join(",", section.Delimiters.Select(ToLiteral))}]";
                                tw.AppendLine($"foreach(var split in ParseHelpers.Chunk({delims}, section))");
                                tw.AppendLine("{").Indent();
                                toParse = "split";
                            }
                            //Parse the section
                            tw.AppendLine($"if({section.SectionType}.TryParse({toParse}, out var val, stringPool, properties))");
                            using (tw.Block())
                            {
                                if (section.Child)
                                {
                                    if (section.List)
                                    {
                                        tw.AppendLine(
                                            $"if({section.FieldName}.Count < 1 || !{section.FieldName}[^1].TryAddChildSection(section.Name, val))");
                                        using (tw.Block())
                                        {
                                            tw.AppendLine($"IniDiagnostic.ChildAddFailure({toParse});");
                                        }
                                    }
                                    else
                                    {
                                        tw.AppendLine(
                                            $"if(!({section.FieldName}?.TryAddChildSection(section.Name, val) ?? false))");
                                        using (tw.Block())
                                        {
                                            tw.AppendLine($"IniDiagnostic.ChildAddFailure({toParse});");
                                        }
                                    }
                                }
                                else if (section.List)
                                {
                                    tw.AppendLine($"{section.FieldName} ??= new();");
                                    tw.AppendLine($"{section.FieldName}.Add(val);");
                                }
                                else
                                {
                                    tw.AppendLine($"{section.FieldName} = val;");
                                }
                            }
                            // If we split the section
                            if (section.Delimiters.Count > 0)
                            {
                                tw.UnIndent().AppendLine("}");
                            }
                            tw.UnIndent().AppendLine("}");
                            tw.AppendLine("break;");
                        }
                        tw.AppendLine("default:");
                        tw.Indent()
                            .AppendLine("IniDiagnostic.UnknownSection(section);")
                            .AppendLine("break;")
                            .UnIndent();
                    }
                }
                tw.AppendLine("if(closeStreams) src.Stream.Dispose();");
            }
        }
        tw.UnIndent().AppendLine("}");
        context.AddSource($"{ini.Namespace}.{ini.Name}.I.Parse.g.cs",
            SourceText.From(tw.ToString(), Encoding.UTF8));
    }
}
