using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using LibreLancer.GeneratorCommon;
using Microsoft.CodeAnalysis.CSharp;

namespace LibreLancer.Data.Generator
{
    [Generator]
    public class ParserGenerator : IIncrementalGenerator
    {
        static EntryType FromTypeSymbol(ITypeSymbol typeSymbol, INamedTypeSymbol? parent)
        {
            if (typeSymbol is IArrayTypeSymbol arrayType)
            {
                var elemType = FromTypeSymbol(arrayType.ElementType, null);
                return new EntryType(elemType.Type, false, true, elemType.EnumName);
            }

            var symbol = (INamedTypeSymbol)typeSymbol;
            if (symbol.TryGetNullableValueUnderlyingType(out var underlyingType))
            {
                return FromTypeSymbol((INamedTypeSymbol)underlyingType, null);
            }
            switch (symbol.SpecialType)
            {
                case SpecialType.System_String:
                    return EntryType.Basic(SupportedType.String);
                case SpecialType.System_Single:
                    return EntryType.Basic(SupportedType.Float);
                case SpecialType.System_Boolean:
                    return EntryType.Basic(SupportedType.Boolean);
                case SpecialType.System_Int32:
                    return EntryType.Basic(SupportedType.Int);
                case SpecialType.System_Int64:
                    return EntryType.Basic(SupportedType.Long);
            }

            if (symbol.IsEnum())
            {
                return new(SupportedType.Enum, false, false, symbol.ToString());
            }

            switch (symbol.ToString())
            {
                case "System.Numerics.Quaternion":
                    return EntryType.Basic(SupportedType.Quaternion);
                case "System.Numerics.Vector4":
                    return EntryType.Basic(SupportedType.Vector4);
                case "System.Numerics.Vector3":
                    return EntryType.Basic(SupportedType.Vector3);
                case "System.Numerics.Vector2":
                    return EntryType.Basic(SupportedType.Vector2);
                case "System.Guid":
                    return EntryType.Basic(SupportedType.Guid);
                case "LibreLancer.Color4":
                    return EntryType.Basic(SupportedType.Color4);
                case "LibreLancer.Color3f":
                    return EntryType.Basic(SupportedType.Color3f);
                case "LibreLancer.Data.HashValue":
                    return EntryType.Basic(SupportedType.HashValue);
                case "LibreLancer.Data.ValueRange<int>":
                    return EntryType.Basic(SupportedType.ValueRangeInt);
                case "LibreLancer.Data.ValueRange<float>":
                    return EntryType.Basic(SupportedType.ValueRangeFloat);
            }

            if (parent == null && symbol.ToString().StartsWith("System.Collections.Generic.List<"))
            {
                var basic = FromTypeSymbol(symbol.TypeArguments[0], parent);
                return new(basic.Type, true, basic.Array, basic.EnumName);
            }

            throw new Exception($"Type {parent ?? symbol} is not supported for parsing entries");

        }

        static string OptionalString(object? optional) => optional?.ToString() ?? "";

        // determine the namespace the class/enum/struct is declared in, if any
        static string GetNamespace(BaseTypeDeclarationSyntax syntax)
        {
            // If we don't have a namespace at all we'll return an empty string
            // This accounts for the "default namespace" case
            string nameSpace = string.Empty;

            // Get the containing syntax node for the type declaration
            // (could be a nested type, for example)
            SyntaxNode? potentialNamespaceParent = syntax.Parent;

            // Keep moving "out" of nested classes etc until we get to a namespace
            // or until we run out of parents
            while (potentialNamespaceParent != null &&
                   potentialNamespaceParent is not NamespaceDeclarationSyntax
                   && potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax)
            {
                potentialNamespaceParent = potentialNamespaceParent.Parent;
            }

            // Build up the final namespace by looping until we no longer have a namespace declaration
            if (potentialNamespaceParent is BaseNamespaceDeclarationSyntax namespaceParent)
            {
                // We have a namespace. Use that as the type
                nameSpace = namespaceParent.Name.ToString();

                // Keep moving "out" of the namespace declarations until we
                // run out of nested namespace declarations
                while (true)
                {
                    if (namespaceParent.Parent is not NamespaceDeclarationSyntax parent)
                    {
                        break;
                    }

                    // Add the outer namespace as a prefix to the final namespace
                    nameSpace = $"{namespaceParent.Name}.{nameSpace}";
                    namespaceParent = parent;
                }
            }

            // return the final namespace
            return nameSpace;
        }

        enum ListKind
        {
            Array,
            List,
            None
        }

        static ITypeSymbol Simplify(ITypeSymbol symbol)
            => symbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated);

        static (ListKind Kind, string TypeName) GetListType(ITypeSymbol typeSymbol)
        {
            typeSymbol = Simplify(typeSymbol);
            if (typeSymbol is IArrayTypeSymbol arrayType)
            {
                return (ListKind.Array, Simplify(arrayType.ElementType).ToString());
            }
            else if (typeSymbol is INamedTypeSymbol namedType)
            {
                if (namedType.ToString().StartsWith("System.Collections.Generic.List<"))
                {
                    return (ListKind.List, Simplify(namedType.TypeArguments[0]).ToString());
                }
                else
                {
                    return (ListKind.None, namedType.ToString());
                }
            }
            return (ListKind.None, typeSymbol.ToString());
        }



        static ParsedSectionInfo SectionTransform(GeneratorAttributeSyntaxContext context,
            CancellationToken cancelToken)
        {
            var sectionType = (ClassDeclarationSyntax)context.TargetNode;
            var sectionSymbol = context.SemanticModel.GetDeclaredSymbol(sectionType)!;
            List<Entry> entries = new();
            List<EntryHandler> handlers = new();
            List<Section> children = new();
            bool isEntryHandler = sectionSymbol.AllInterfaces.Any(x => x.ToString() == "LibreLancer.Data.Ini.IEntryHandler");
            string? onParseDependent = null;
            bool hasBaseSection = sectionSymbol.BaseType!.GetAttributes()
                .Any(x => "LibreLancer.Data.Ini.BaseSectionAttribute".Equals(x.AttributeClass?.ToString()));
            bool isBaseSection = sectionSymbol!.GetAttributes()
                .Any(x => "LibreLancer.Data.Ini.BaseSectionAttribute".Equals(x.AttributeClass?.ToString()));
            ISymbol[] allMembers = hasBaseSection
                ? sectionSymbol.GetMembers().ToArray()
                : sectionSymbol.GetBaseTypesAndThis().SelectMany(x => x.GetMembers()).ToArray();
            foreach (var field in allMembers.OfType<IFieldSymbol>())
            {
                var entryAttrs = field.GetAttributes()
                    .Where(x => "LibreLancer.Data.Ini.EntryAttribute"
                    .Equals(x.AttributeClass?.ToString())).ToArray();
                if (entryAttrs.Length == 0)
                {
                    var sectionAttrs = field.GetAttributes()
                        .Where(x => "LibreLancer.Data.Ini.SectionAttribute"
                            .Equals(x.AttributeClass?.ToString())).ToArray();
                    if (sectionAttrs.Length == 0)
                    {
                        continue;
                    }
                    var (fieldKind, fieldListType) = GetListType(field.Type);
                    foreach (var s in sectionAttrs)
                    {
                        var args = new AttributeArguments(s.NamedArguments);
                        bool child = args.Boolean("Child");
                        if (!child)
                            continue; //Not relevant
                        if (fieldKind != ListKind.Array)
                        {
                            children.Add(new Section(OptionalString(s.ConstructorArguments[0].Value), field.Name,
                                fieldListType, fieldListType, true, fieldKind == ListKind.List, EquatableArray<string>.Empty));
                        }
                    }
                    continue;
                }
                var fieldType = FromTypeSymbol(field.Type, null);
                foreach (var e in entryAttrs)
                {
                    var args = new AttributeArguments(e.NamedArguments);
                    bool required = args.Boolean("Required");
                    bool multiline = args.Boolean("Multiline");
                    bool minMax = args.Boolean("MinMax");
                    bool presence = args.Boolean("Presence");
                    bool floatColor = args.Boolean("FloatColor");
                    Vec3Mode vec3Mode = (Vec3Mode)args.Integer("Mode");
                    entries.Add(new(
                        OptionalString(e.ConstructorArguments[0].Value),
                        field.Name,
                        fieldType,
                        multiline,
                        required,
                        minMax,
                        presence,
                        floatColor,
                        vec3Mode));
                }
            }

            foreach (var method in allMembers.OfType<IMethodSymbol>())
            {
                var attrs = method.GetAttributes()
                    .Where(x => "LibreLancer.Data.Ini.EntryHandlerAttribute"
                        .Equals(x.AttributeClass?.ToString())).ToArray();
                if (attrs.Length == 0)
                {
                    if (onParseDependent == null)
                    {
                        var dependent = method.GetAttributes()
                            .Any(x => "LibreLancer.Data.Ini.OnParseDependentAttribute"
                                .Equals(x.AttributeClass?.ToString()));
                        if (dependent)
                        {
                            onParseDependent = method.Name;
                        }
                    }
                    continue;
                }
                foreach (var e in attrs)
                {
                    var args = new AttributeArguments(e.NamedArguments);
                    bool multiline = args.Boolean("Multiline");
                    int minComponents = args.Integer("MinComponents");
                    handlers.Add(new(OptionalString(e.ConstructorArguments[0].Value), method.Name, multiline, minComponents));
                }
            }

            return new ParsedSectionInfo(
                GetNamespace(sectionType),
                sectionType.Identifier.Text,
                isEntryHandler,
                isBaseSection,
                hasBaseSection,
                onParseDependent,
                new EquatableArray<Entry>(entries.ToArray()),
                new EquatableArray<EntryHandler>(handlers.ToArray()),
                new EquatableArray<Section>(children.ToArray())
                );
        }


        static ParsedIniInfo IniTransform(GeneratorAttributeSyntaxContext context,
            CancellationToken cancelToken)
        {
            var iniType = (ClassDeclarationSyntax)context.TargetNode;
            var iniSymbol = context.SemanticModel.GetDeclaredSymbol(iniType)!;
            List<Section> sections = new();
            List<string> ignores = new();
            var allMembers = iniSymbol.GetBaseTypesAndThis().SelectMany(x => x.GetMembers()).ToArray();
            var ignoreAttrs = iniSymbol.GetAttributes().Where(x => "LibreLancer.Data.Ini.IgnoreSectionAttribute".Equals(x.AttributeClass?.ToString()));
            foreach (var ig in ignoreAttrs)
            {
                ignores.Add(OptionalString(ig.ConstructorArguments[0].Value));
            }

            bool preparse = true;
            var parseAttr = iniSymbol.GetAttributes().First(x => "LibreLancer.Data.Ini.ParsedIniAttribute".Equals(x.AttributeClass?.ToString()));
            var preparseArg = parseAttr.NamedArguments.FirstOrDefault(x => x.Key == "Preparse");
            if (preparseArg.Key == "Preparse")
            {
                preparse = preparseArg.Value.Value == null || ((bool)preparseArg.Value.Value);
            }
            foreach (var field in allMembers.OfType<IFieldSymbol>())
            {
                var sectionAttrs = field.GetAttributes()
                    .Where(x => "LibreLancer.Data.Ini.SectionAttribute"
                        .Equals(x.AttributeClass?.ToString())).ToArray();
                if (sectionAttrs.Length == 0)
                {
                    continue;
                }

                var (fieldKind, fieldListType) = GetListType(field.Type);
                if (fieldKind == ListKind.Array) // We don't support arrays
                    continue;
                foreach (var s in sectionAttrs)
                {
                    var args = new AttributeArguments(s.NamedArguments);
                    bool child = args.Boolean("Child");
                    string type = args.String("Type");
                    if (string.IsNullOrEmpty(type)
                        && s.ConstructorArguments.Length > 1)
                    {
                        type = OptionalString(s.ConstructorArguments[1].Value);
                    }
                    var delimiters = args.StringArray("Delimiters");
                    sections.Add(new Section(OptionalString(s.ConstructorArguments[0].Value), field.Name,
                        fieldListType, string.IsNullOrEmpty(type) ? fieldListType : type, child, fieldKind == ListKind.List, delimiters));
                }
            }
            return new ParsedIniInfo(
                GetNamespace(iniType),
                iniType.Identifier.Text,
                preparse,
                new EquatableArray<Section>(sections.ToArray()),
                new EquatableArray<string>(ignores.ToArray()));
        }



        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var sections = context.SyntaxProvider.ForAttributeWithMetadataName(
                "LibreLancer.Data.Ini.ParsedSectionAttribute",
                predicate: (node, _) => node is ClassDeclarationSyntax,
                transform: SectionTransform
            );

            var baseSection = context.SyntaxProvider.ForAttributeWithMetadataName(
                "LibreLancer.Data.Ini.BaseSectionAttribute",
                predicate: (node, _) => node is ClassDeclarationSyntax,
                transform: SectionTransform
            );

            var inis = context.SyntaxProvider.ForAttributeWithMetadataName(
                "LibreLancer.Data.Ini.ParsedIniAttribute",
                predicate: (node, _) => node is ClassDeclarationSyntax,
                transform: IniTransform
            );

            context.RegisterSourceOutput(baseSection, SectionParser.GenerateBaseSectionParser);
            context.RegisterSourceOutput(sections, SectionParser.GenerateSectionParser);
            context.RegisterSourceOutput(inis, IniParser.GenerateIniParser);
        }
    }
}
