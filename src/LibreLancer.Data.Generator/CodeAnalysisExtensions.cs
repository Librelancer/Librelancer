using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace LibreLancer.Data.Generator;

public static class CodeAnalysisExtensions
{
    public static bool TryGetNullableValueUnderlyingType(this ITypeSymbol typeSymbol,
        [NotNullWhen(returnValue: true)] out ITypeSymbol? underlyingType)
    {
        if (typeSymbol is INamedTypeSymbol namedType && typeSymbol.IsValueType && namedType.IsGenericType)
        {
            var typeParameters = namedType.TypeArguments;
            if (namedType.ConstructUnboundGenericType() is { Name: "Nullable" } genericType &&
                genericType.ContainingNamespace.Name == "System" && genericType.TypeArguments.Length == 1)
            {
                underlyingType = typeParameters[0];
                return underlyingType.TypeKind != TypeKind.Error;
            }
        }
        underlyingType = null;
        return false;
    }

    public static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(this ITypeSymbol type)
    {
        var current = type;
        while (current != null)
        {
            yield return current;
            current = current.BaseType;
        }
    }

    public static bool IsEnum(this ITypeSymbol typeSymbol) =>
        typeSymbol is INamedTypeSymbol namedType && namedType.EnumUnderlyingType != null;
}
