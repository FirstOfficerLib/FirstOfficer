using Microsoft.CodeAnalysis;

namespace FirstOfficer.Extensions
{
    internal static class NamedTypeSymbolExtensions
    {

        internal static string FullName(this INamedTypeSymbol typeSymbol)
        {
            var format = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
            return typeSymbol.ToDisplayString(format);  //maybe add "global::" to the beginning
        }

        internal static string Namespace(this INamedTypeSymbol typeSymbol)
        {
            return typeSymbol.ContainingNamespace.ToDisplayString();
        }


    }
}
