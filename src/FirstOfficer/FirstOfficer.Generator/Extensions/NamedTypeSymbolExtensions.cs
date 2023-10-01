using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace FirstOfficer.Generator.Extensions
{
    internal static class NamedTypeSymbolExtensions
    {

        internal static string FullName(this INamedTypeSymbol typeSymbol)
        {
            var format = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
            return typeSymbol.ToDisplayString(format);
        }


    }
}
