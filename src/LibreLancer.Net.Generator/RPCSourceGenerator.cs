using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
namespace LibreLancer.Net.Generator;

[Generator]
public class RPCSourceGenerator : IIncrementalGenerator
{

    static string CSTypeName(ITypeSymbol type)
    {
        return type.ToDisplayString(new SymbolDisplayFormat(
            SymbolDisplayGlobalNamespaceStyle.Omitted,
            SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable
        ));
    }

    public static bool IsEnum(ITypeSymbol typeSymbol) =>
        typeSymbol is INamedTypeSymbol namedType && namedType.EnumUnderlyingType != null;

    static RPCType FromTypeSymbol(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is INamedTypeSymbol ts && ts.ToString().StartsWith("System.Threading.Tasks.Task<"))
        {
            var basic = FromTypeSymbol(ts.TypeArguments[0]);
            return basic with { Task = true };
        }

        if (typeSymbol is IArrayTypeSymbol arrayType)
        {
            var elemType = FromTypeSymbol(arrayType.ElementType);
            return elemType with { Array = true };
        }

        return new(CSTypeName(typeSymbol), false, IsEnum(typeSymbol), false);
    }

    static RPCInterface InterfaceTransform(GeneratorAttributeSyntaxContext context,
        CancellationToken cancelToken)
    {
        var ifcSymbol = (INamedTypeSymbol)context.SemanticModel.GetDeclaredSymbol(context.TargetNode)!;

        var methods = new List<RPCMethod>();
        foreach (var method in ifcSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            var rType = method.ReturnsVoid ? new RPCType("void", false, false, false) : FromTypeSymbol(method.ReturnType);
            var p = new List<RPCParameter>();
            foreach (var param in method.Parameters)
            {
                p.Add(new(param.Name, FromTypeSymbol(param.Type)));
            }
            int channel = 0;
            foreach (var att in method.GetAttributes())
            {
                if ("LibreLancer.Net.Protocol.ChannelAttribute".Equals(att.AttributeClass?.ToString()))
                {
                    channel = (int) (att.ConstructorArguments[0].Value ?? 0);
                    break;
                }
            }
            methods.Add(new(method.Name, channel, rType, new(p.ToArray())));
        }
        return new(ifcSymbol.Name, ifcSymbol.ContainingNamespace?.ToDisplayString(), new(methods.ToArray()));
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var interfaces = context.SyntaxProvider.ForAttributeWithMetadataName(
            "LibreLancer.Net.Protocol.RPCInterfaceAttribute",
            predicate: (node, _) => node is InterfaceDeclarationSyntax,
            transform: InterfaceTransform
        );

        var responseTypes =
            interfaces.SelectMany((x, _) => x.Methods)
            .Where(x => x.ReturnType.Name != "void")
            .Select((x, _) => x.ReturnType)
            .Collect();

        context.RegisterSourceOutput(interfaces, RPCSourceWriter.GenerateRPCInterface);
        context.RegisterSourceOutput(interfaces.Collect(), RPCSourceWriter.GenerateProtocol);
        context.RegisterSourceOutput(responseTypes, RPCSourceWriter.GenerateResponses);
    }
}
