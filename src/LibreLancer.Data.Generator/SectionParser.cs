using System;
using System.Linq;
using System.Text;
using LibreLancer.GeneratorCommon;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace LibreLancer.Data.Generator;

public static class SectionParser
{
    static void ErrorChecking(TabbedWriter tw, int index, string? requiredBlock)
    {
        if (!string.IsNullOrWhiteSpace(requiredBlock))
        {
            tw.AppendLine(requiredBlock!);
        }

        if (index >= 0)
        {
            var bit = (1UL << index);
            tw.AppendLine($"if((isSet & 0x{bit:X}UL) != 0)");
            using (tw.Block())
            {
                tw.AppendLine("IniDiagnostic.DuplicateEntry(entry, section);");
            }

            tw.AppendLine($"isSet |= 0x{bit:X}UL;");
        }
    }

    static string CsTypeName(SupportedType st) => st switch
    {
        SupportedType.Float => "float",
        SupportedType.Int => "int",
        SupportedType.String => "string",
        SupportedType.Long => "long",
        SupportedType.Boolean => "bool",
        _ => "UNSUPPORTED"
    };


    static void ParseField(TabbedWriter tw, Entry e, int index, string? requiredBlock, bool isInstance)
    {
        if (e.Type.Array)
        {
            tw.AppendLine("if (ParseHelpers.ComponentCheck(int.MaxValue, section, entry, 1))");
            using (tw.Block())
            {
                tw.AppendLine($"var array = new {CsTypeName(e.Type.Type)}[entry.Count];");
                tw.AppendLine("for(int i = 0; i < entry.Count; i++)");
                using (tw.Block())
                {
                    switch (e.Type.Type)
                    {
                        case SupportedType.String:
                            tw.AppendLine($"array[i] = entry[i].ToString()!;");
                            break;
                        case SupportedType.Float:
                            tw.AppendLine($"array[i] = entry[i].ToSingle();");
                            break;
                        case SupportedType.Int:
                            tw.AppendLine($"array[i] = entry[i].ToInt32();");
                            break;
                        case SupportedType.Long:
                            tw.AppendLine($"array[i] = entry[i].ToInt64();");
                            break;
                        case SupportedType.Boolean:
                            tw.AppendLine($"array[i] = entry[i].ToBoolean();");
                            break;
                    }
                }
                SetValue("array");
            }
            ErrorChecking(tw, e.Type.List ? -1 : index, requiredBlock);
            return;
        }
        else if (e.Type.List && !e.Multiline)
        {
            ErrorChecking(tw, index, requiredBlock);
            return;
        }

        void SetValue(string value)
        {
            string target = isInstance ? "" : "result.";
            if (e.Type.List && (e.Multiline || e.Type.Array))
            {
                tw.AppendLine($"{target}{e.FieldName} ??= new();");
                tw.AppendLine($"{target}{e.FieldName}.Add({value});");
            }
            else
            {
                tw.AppendLine($"{target}{e.FieldName} = {value};");
            }
        }

        switch (e.Type.Type)
        {
            case SupportedType.String:
                if (!e.Presence)
                {
                    tw.AppendLine("if (ParseHelpers.ComponentCheck(1, section, entry))");
                }
                using (tw.Block())
                {
                    if (e.Presence)
                    {
                        SetValue("(entry.Count > 0 ? entry[0].ToString()! : \"\")");
                    }
                    else
                    {
                        SetValue("entry[0].ToString()!");
                    }

                    ErrorChecking(tw, index, requiredBlock);
                }
                break;
            case SupportedType.Boolean:
                if (!e.Presence)
                {
                    tw.AppendLine("if (ParseHelpers.ComponentCheck(1, section, entry))");
                }
                using (tw.Block())
                {
                    if (e.Presence)
                    {
                        SetValue("(entry.Count > 0 ? entry[0].ToBoolean() : true)");
                    }
                    else
                    {
                        SetValue("entry[0].ToBoolean()");
                    }

                    ErrorChecking(tw, index, requiredBlock);
                }

                break;
            case SupportedType.Enum:
                tw.AppendLine("if (ParseHelpers.ComponentCheck(1, section, entry))");
                using (tw.Block())
                {
                    tw.AppendLine($"if (Enum.TryParse<{e.Type.EnumName}>(entry[0].ToString()!, true, out var ev))");
                    using (tw.Block())
                    {
                        SetValue("ev");
                        ErrorChecking(tw, index, requiredBlock);
                    }
                    tw.AppendLine("else");
                    using (tw.Block())
                    {
                        tw.AppendLine("IniDiagnostic.InvalidEnum(entry, section);");
                    }
                }
                break;
            case SupportedType.Float:
                tw.AppendLine("if (ParseHelpers.ComponentCheck(1, section, entry))");
                using (tw.Block())
                {
                    SetValue("entry[0].ToSingle()");
                    ErrorChecking(tw, index, requiredBlock);
                }

                break;
            case SupportedType.Int:
                tw.AppendLine("if (ParseHelpers.ComponentCheck(1, section, entry))");
                using (tw.Block())
                {
                    SetValue("entry[0].ToInt32()");
                    ErrorChecking(tw, index, requiredBlock);
                }

                break;
            case SupportedType.Long:
                tw.AppendLine("if (ParseHelpers.ComponentCheck(1, section, entry))");
                using (tw.Block())
                {
                    SetValue("entry[0].ToInt64()");
                    ErrorChecking(tw, index, requiredBlock);
                }

                break;
            case SupportedType.Vector2:
                if (e.MinMax)
                {
                    tw.AppendLine("if (entry.Count == 1)");
                    using (tw.Block())
                    {
                        SetValue("new Vector2(-1, entry[0].ToSingle())");
                    }
                    tw.AppendLine("else if (ParseHelpers.ComponentCheck(2, section, entry))");
                    using (tw.Block())
                    {
                        SetValue("new Vector2(entry[0].ToSingle(), entry[1].ToSingle())");
                    }
                    ErrorChecking(tw, index, requiredBlock);
                }
                else
                {
                    tw.AppendLine("if (ParseHelpers.ComponentCheck(2, section, entry))");
                    using (tw.Block())
                    {
                        SetValue("new Vector2(entry[0].ToSingle(), entry[1].ToSingle())");
                        ErrorChecking(tw, index, requiredBlock);
                    }
                }
                break;
            case SupportedType.Vector3:
                tw.AppendLine("if (entry.Count == 1 && entry[0].ToSingle() == 0)");
                using (tw.Block())
                {
                    SetValue("Vector3.Zero");
                }
                switch (e.Vec3Mode)
                {
                    case Vec3Mode.Size:
                        tw.AppendLine("else if (ParseHelpers.ComponentCheck(3, section, entry, 1))");
                        using (tw.Block())
                        {
                            tw.AppendLine("if(entry.Count == 1)");
                            using (tw.Block())
                            {
                                SetValue("new Vector3(entry[0].ToSingle())");
                            }
                            tw.AppendLine("else if (entry.Count == 2)");
                            using (tw.Block())
                            {
                                SetValue("new Vector3(entry[0].ToSingle(), entry[1].ToSingle(), 0)");
                            }
                            tw.AppendLine("else");
                            using (tw.Block())
                            {
                                SetValue("new Vector3(entry[0].ToSingle(), entry[1].ToSingle(), entry[2].ToSingle())");
                            }
                        }
                        break;
                    case Vec3Mode.OptionalComponents:
                        tw.AppendLine("else if (ParseHelpers.ComponentCheck(3, section, entry, 1))");
                        using (tw.Block())
                        {
                            tw.AppendLine("if(entry.Count == 1)");
                            using (tw.Block())
                            {
                                SetValue("new Vector3(entry[0].ToSingle(), 0, 0)");
                            }
                            tw.AppendLine("else if (entry.Count == 2)");
                            using (tw.Block())
                            {
                                SetValue("new Vector3(entry[0].ToSingle(), entry[1].ToSingle(), 0)");
                            }
                            tw.AppendLine("else");
                            using (tw.Block())
                            {
                                SetValue("new Vector3(entry[0].ToSingle(), entry[1].ToSingle(), entry[2].ToSingle())");
                            }
                        }
                        break;
                    case Vec3Mode.None:
                        tw.AppendLine("else if (ParseHelpers.ComponentCheck(3, section, entry))");
                        using (tw.Block())
                        {
                            SetValue("new Vector3(entry[0].ToSingle(), entry[1].ToSingle(), entry[2].ToSingle())");
                        }
                        break;
                }
                ErrorChecking(tw, index, requiredBlock);
                break;
            case SupportedType.Vector4:
                tw.AppendLine("if (ParseHelpers.ComponentCheck(4, section, entry))");
                using (tw.Block())
                {
                    SetValue(
                        "new Vector4(entry[0].ToSingle(), entry[1].ToSingle(), entry[2].ToSingle(), entry[3].ToSingle())");
                    ErrorChecking(tw, index, requiredBlock);
                }
                break;
            case SupportedType.Quaternion:
                tw.AppendLine("if (ParseHelpers.ComponentCheck(4, section, entry))");
                using (tw.Block())
                {
                    SetValue(
                        "new Quaternion(entry[1].ToSingle(), entry[2].ToSingle(), entry[3].ToSingle(), entry[0].ToSingle())");
                    ErrorChecking(tw, index, requiredBlock);
                }
                break;
            case SupportedType.Color4:
                tw.AppendLine("if (ParseHelpers.ComponentCheck(4, section, entry, 3))");
                using (tw.Block())
                {
                    tw.AppendLine("if (entry.Count == 3)");
                    using (tw.Block())
                    {
                        SetValue("new Color4(entry[0].ToSingle() / 255f, entry[1].ToSingle() / 255f, entry[2].ToSingle() / 255f, 1)");
                    }
                    tw.AppendLine("else");
                    using (tw.Block())
                    {
                        SetValue("new Color4(entry[0].ToSingle() / 255f, entry[1].ToSingle() / 255f, entry[2].ToSingle() / 255f, entry[3].ToSingle() / 255f)");
                    }
                    ErrorChecking(tw, index, requiredBlock);
                }
                break;
            case SupportedType.Color3f:
                tw.AppendLine("if (ParseHelpers.ComponentCheck(3, section, entry))");
                using (tw.Block())
                {
                    if (e.FloatColor)
                    {
                        SetValue("new Color3f(entry[0].ToSingle(), entry[1].ToSingle(), entry[2].ToSingle())");
                        ErrorChecking(tw, index, requiredBlock);
                    }
                    else
                    {
                        SetValue("new Color3f(entry[0].ToSingle() / 255f, entry[1].ToSingle() / 255f, entry[2].ToSingle() / 255f)");
                        ErrorChecking(tw, index, requiredBlock);
                    }
                }
                break;
            case SupportedType.Guid:
                tw.AppendLine("if (ParseHelpers.ComponentCheck(1, section, entry))");
                using (tw.Block())
                {
                    tw.AppendLine("if(Guid.TryParse(entry[0].ToString()!, out var g))");
                    using (tw.Block())
                    {
                        SetValue("g");
                        ErrorChecking(tw, index, requiredBlock);
                    }
                    tw.AppendLine("else");
                    using (tw.Block())
                    {
                        tw.AppendLine("IniDiagnostic.InvalidGuid(entry, section);");
                    }
                }
                break;
            case SupportedType.HashValue:
                tw.AppendLine("if (ParseHelpers.ComponentCheck(1, section, entry))");
                using (tw.Block())
                {
                    SetValue("new HashValue(entry[0])");
                    ErrorChecking(tw, index, requiredBlock);
                }
                break;
            case SupportedType.ValueRangeFloat:
                tw.AppendLine("if (ParseHelpers.ComponentCheck(2, section, entry))");
                using (tw.Block())
                {
                    SetValue("new ValueRange<float>(entry[0].ToSingle(), entry[1].ToSingle())");
                    ErrorChecking(tw, index, requiredBlock);
                }
                break;
            case SupportedType.ValueRangeInt:
                tw.AppendLine("if (ParseHelpers.ComponentCheck(2, section, entry))");
                using (tw.Block())
                {
                    SetValue("new ValueRange<int>(entry[0].ToInt32(), entry[1].ToInt32())");
                    ErrorChecking(tw, index, requiredBlock);
                }
                break;
        }
    }

    static string ToLiteral(string input)
    {
        return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(input))
            .ToFullString();
    }

    static void BoilerPlate(TabbedWriter tw, ParsedSectionInfo section)
    {
        tw.AppendLine("using System;");
        tw.AppendLine("using System.Collections.Generic;");
        tw.AppendLine("using System.Diagnostics.CodeAnalysis;");
        tw.AppendLine("using System.Numerics;");
        if (!section.Namespace.StartsWith("LibreLancer"))
        {
            tw.AppendLine("using LibreLancer;");
        }
        tw.AppendLine("using LibreLancer.Data.Ini;");
        tw.AppendLine("using LibreLancer.Data;");
        tw.AppendLine();
        tw.AppendLine("#nullable enable");
        tw.AppendLine();
        tw.AppendLine($"namespace {section.Namespace};");
        tw.AppendLine();
        tw.AppendLine($"partial class {section.Name}");
        tw.AppendLine("{").Indent();
    }


    public static void GenerateBaseSectionParser(SourceProductionContext context, ParsedSectionInfo section)
    {
        var entries = section.Entries.AsSpan();
        var entryHandlers = section.Handlers.AsSpan();
        var children = section.Children.AsSpan();
        ulong required = 0;
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].Required)
            {
                required |= (1UL << i);
            }
        }
        TabbedWriter tw = new();
        BoilerPlate(tw, section);

        var mod = section.HasBaseSection ? "override " : "virtual ";
        if(children.Length > 0 || !section.HasBaseSection)
            GenerateTryAddChild(tw, mod, section, children);

        tw.AppendEditorHiddenLine()
            .AppendLine($"protected {mod}void _SetBaseRequired(Span<ulong> requireds, int index)");
        using (tw.Block())
        {
            if (section.HasBaseSection)
            {
                tw.AppendLine("base._SetBaseRequired(requireds, index + 1);");
            }
            tw.AppendLine($"requireds[index] = 0x{required:X}UL;");
        }

        tw.AppendEditorHiddenLine()
            .AppendLine($"protected {mod}bool _ParseBase(uint hash, Entry entry, Section section, Span<ulong> requireds, Span<ulong> sets, int index)");
        using (tw.Block())
        {
            if (section.HasBaseSection)
            {
                tw.AppendLine("if(base._ParseBase(hash, entry, section, requireds, sets, index + 1)) return true;");
            }
            tw.AppendLine("ref ulong required = ref requireds[index];");
            tw.AppendLine("ref ulong isSet = ref sets[index];");
            tw.AppendLine("switch (hash)");
            using (tw.Block())
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    var e = entries[i];
                    tw.AppendLine($"case 0x{IniHash.Hash(e.EntryName):X}:");
                    tw.AppendLine(
                        $"if({ToLiteral(e.EntryName)}.Equals(entry.Name, StringComparison.OrdinalIgnoreCase))");
                    using (tw.Block())
                    {
                        string? requiredBlock = entries[i].Required
                            ? $"required &= ~(0x{(1UL << i):X}UL);"
                            : null;
                        ParseField(tw, e, e.Multiline ? -1 : i, requiredBlock, true);
                    }

                    tw.AppendLine("return true;");
                }
                for (int i = 0; i < entryHandlers.Length; i++)
                {
                    var e = entryHandlers[i];
                    tw.AppendLine($"case 0x{IniHash.Hash(e.EntryName):X}:");
                    tw.AppendLine(
                        $"if({ToLiteral(e.EntryName)}.Equals(entry.Name, StringComparison.OrdinalIgnoreCase))");
                    using (tw.Block())
                    {
                        if (e.Components > 0)
                        {
                            tw.AppendLine(
                                $"if(ParseHelpers.ComponentCheck(int.MaxValue, section, entry, {e.Components}))");
                            using (tw.Block())
                            {
                                tw.AppendLine($"{e.MethodName}(entry);");
                            }
                        }
                        else
                        {
                            tw.AppendLine($"{e.MethodName}(entry);");
                        }
                        if (!e.Multiline)
                        {
                            ErrorChecking(tw, i + entries.Length, null);
                        }
                    }
                    tw.AppendLine("return true;");
                }
                // on default: fall-through to og parsing
                tw.AppendLine("default:");
                tw.Indent().AppendLine("return false;").UnIndent();
            }
        }

        // Generate cascading check of required fields
        if (required > 0 || !section.HasBaseSection)
        {
            tw.AppendEditorHiddenLine()
                .AppendLine(
                    $"protected {mod}void _BaseRequiredError(Section section, Span<ulong> requireds, int index, ref bool isError)");
            using (tw.Block())
            {
                if (section.HasBaseSection)
                {
                    tw.AppendLine("base._BaseRequiredError(section, requireds, index + 1, ref isError);");
                }
                if (required > 0)
                {
                    tw.AppendLine("if(requireds[index] == 0) return;");
                    for (int i = 0; i < entries.Length; i++)
                    {
                        var e = entries[i];
                        if (!e.Required) continue;
                        tw.AppendLine($"if((requireds[index] & (0x{(1UL << i):X}UL)) != 0)");
                        using (tw.Block())
                        {
                            tw.AppendLine("isError = true;");
                            tw.AppendLine($"IniDiagnostic.MissingField({ToLiteral(e.EntryName)}, section);");
                        }
                    }
                }
            }
        }
        tw.UnIndent().AppendLine("}");
        context.AddSource($"{section.Namespace}.{section.Name}.B.Parse.g.cs",
            SourceText.From(tw.ToString(), Encoding.UTF8));
    }

    static void GenerateTryAddChild(TabbedWriter tw, string addChildMod, ParsedSectionInfo section, ReadOnlySpan<Section> children)
    {
        tw.AppendLine($"public {addChildMod}bool TryAddChildSection(string sectionName, object parsedChild)");
        using (tw.Block())
        {
            if (section.HasBaseSection)
            {
                tw.AppendLine("if (base.TryAddChildSection(sectionName, parsedChild)) return true;");
            }
            tw.AppendLine("var hash = ParseHelpers.Hash(sectionName);");
            for(int i = 0; i < children.Length; i++)
            {
                var child = children[i];
                var h = IniHash.Hash(child.SectionName);
                if (i > 0)
                    tw.Append("else ");
                tw.AppendLine(
                    $"if(hash == 0x{h:X} && {ToLiteral(child.SectionName)}.Equals(sectionName, StringComparison.OrdinalIgnoreCase))");
                using (tw.Block())
                {
                    var field = child.FieldType;
                    // If nullable, need to remove the marker
                    if (field.EndsWith("?"))
                    {
                        field.Remove(field.Length - 1);
                    }

                    tw.AppendLine($"if (parsedChild is {field} child)");
                    using (tw.Block())
                    {
                        if (child.List)
                        {
                            tw.AppendLine($"{child.FieldName}.Add(child);");
                        }
                        else
                        {
                            tw.AppendLine($"{child.FieldName} = child;");
                        }
                        tw.AppendLine("return true;");
                    }
                    tw.AppendLine("else");
                    using (tw.Block())
                    {
                        tw.AppendLine("return false;");
                    }
                }

            }
            tw.AppendLine("return false;");
        }
    }

    public static void GenerateSectionParser(SourceProductionContext context, ParsedSectionInfo section)
    {
        var entries = section.Entries.AsSpan();
        var entryHandlers = section.Handlers.AsSpan();
        var children = section.Children.AsSpan();
        ulong required = 0;
        bool checkSet = false;
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].Required)
            {
                required |= (1UL << i);
            }
            if (!entries[i].Multiline)
            {
                checkSet = true; // Avoids unused variable warning
            }
        }

        //
        TabbedWriter tw = new();
        BoilerPlate(tw, section); //Open partial class

        // TryAddChildSection
        string addChildMod = section.HasBaseSection ? "override " : "";
        if (children.Length == 0)
        {
            if (!section.HasBaseSection)
                tw.AppendLine($"public {addChildMod}bool TryAddChildSection(string sectionName, object parsedChild) => false;");
        }
        else if (!section.IsBaseSection)
        {
            GenerateTryAddChild(tw, addChildMod, section, children);
        }
        tw.AppendLine();

        // TryParse
        tw.AppendLine($"public static bool TryParse(Section section, [NotNullWhen(returnValue: true)] out {section.Name}? instance, IniStringPool? stringPool = null, IniParseProperties? properties = null)");
        using (tw.Block())
        {
            if (section.OnParseDependent != null)
            {
                tw.AppendLine("properties ??= IniParseProperties.Empty;");
            }
            tw.AppendLine($"var result = new {section.Name}();");
            if (section.HasBaseSection)
            {
                tw.AppendLine("Span<ulong> baseRequireds = stackalloc ulong[8];");
                tw.AppendLine("Span<ulong> baseSets = stackalloc ulong[8];");
                tw.AppendLine($"result._SetBaseRequired(baseRequireds, 0);");
            }
            if (required != 0)
            {
                tw.AppendLine($"ulong required = 0x{required:X}UL;");
            }
            if (checkSet)
            {
                tw.AppendLine($"ulong isSet = 0;");
            }
            tw.AppendLine("foreach(var entry in section)");
            using (tw.Block())
            {
                tw.AppendLine("var hash = ParseHelpers.Hash(entry.Name);");
                if (section.HasBaseSection)
                {
                    tw.AppendLine("if(result._ParseBase(hash, entry, section, baseRequireds, baseSets, 0))");
                    using (tw.Block())
                    {
                        tw.AppendLine("continue;");
                    }
                }
                tw.AppendLine("switch (hash)");
                using (tw.Block())
                {
                    for (int i = 0; i < entries.Length; i++)
                    {
                        var e = entries[i];
                        tw.AppendLine($"case 0x{IniHash.Hash(e.EntryName):X}:");
                        tw.AppendLine(
                            $"if({ToLiteral(e.EntryName)}.Equals(entry.Name, StringComparison.OrdinalIgnoreCase))");
                        using (tw.Block())
                        {
                            string? requiredBlock = entries[i].Required
                                ? $"required &= ~(0x{(1UL << i):X}UL);"
                                : null;
                            ParseField(tw, e, e.Multiline ? -1 : i, requiredBlock, false);
                        }

                        tw.AppendLine("break;");
                    }

                    for (int i = 0; i < entryHandlers.Length; i++)
                    {
                        var e = entryHandlers[i];
                        tw.AppendLine($"case 0x{IniHash.Hash(e.EntryName):X}:");
                        tw.AppendLine(
                            $"if({ToLiteral(e.EntryName)}.Equals(entry.Name, StringComparison.OrdinalIgnoreCase))");
                        using (tw.Block())
                        {
                            if (e.Components > 0)
                            {
                                tw.AppendLine(
                                    $"if(ParseHelpers.ComponentCheck(int.MaxValue, section, entry, {e.Components}))");
                                using (tw.Block())
                                {
                                    tw.AppendLine($"result.{e.MethodName}(entry);");
                                }
                            }
                            else
                            {
                                tw.AppendLine($"result.{e.MethodName}(entry);");
                            }

                            if (!e.Multiline)
                            {
                                ErrorChecking(tw, i + entries.Length, null);
                            }
                        }

                        tw.AppendLine("break;");
                    }

                    tw.AppendLine("default:");
                    if (section.IsIEntryHandler)
                    {
                        tw.AppendLine("if(!((IEntryHandler)result).HandleEntry(entry))");
                    }
                    using (tw.Block())
                    {
                        tw.AppendLine("IniDiagnostic.UnknownEntry(entry, section);");
                    }
                    tw.AppendLine("break;");
                }
            }
            if (section.HasBaseSection)
            {
                tw.AppendLine("bool _isError = false;");
                tw.AppendLine("result._BaseRequiredError(section, baseRequireds, 0, ref _isError);");
                tw.AppendLine("if(_isError)");
                using(tw.Block())
                {
                    tw.AppendLine("instance = default;");
                    tw.AppendLine("return false;");
                }
            }
            if (required != 0)
            {
                tw.AppendLine("if(required == 0)");
                using (tw.Block())
                {
                    if (section.OnParseDependent != null)
                    {
                        tw.AppendLine($"result.{section.OnParseDependent}(stringPool!, properties);");
                    }
                    tw.AppendLine("instance = result;");
                    tw.AppendLine("return true;");
                }

                tw.AppendLine("else");
                using (tw.Block())
                {
                    for (int i = 0; i < entries.Length; i++)
                    {
                        var e = entries[i];
                        if (!e.Required) continue;
                        tw.AppendLine($"if((required & (0x{(1UL << i):X}UL)) != 0)");
                        using (tw.Block())
                        {
                            tw.AppendLine($"IniDiagnostic.MissingField({ToLiteral(e.EntryName)}, section);");
                        }
                    }

                    tw.AppendLine("instance = default;");
                    tw.AppendLine("return false;");
                }
            }
            else
            {
                if (section.OnParseDependent != null)
                {
                    tw.AppendLine($"result.{section.OnParseDependent}(stringPool!, properties);");
                }
                tw.AppendLine("instance = result;");
                tw.AppendLine("return true;");
            }
        }

        tw.UnIndent().AppendLine("}");
        context.AddSource($"{section.Namespace}.{section.Name}.S.Parse.g.cs",
            SourceText.From(tw.ToString(), Encoding.UTF8));
    }


}
