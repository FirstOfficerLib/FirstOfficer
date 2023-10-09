using Microsoft.CodeAnalysis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using FirstOfficer.Generator.Extensions;

namespace FirstOfficer.Generator.Helpers
{
    internal static class CodeAnalysisHelper
    {
        internal static IPropertySymbol[] GetAllProperties(INamedTypeSymbol entitySymbol,
            List<IPropertySymbol>? props = null!)
        {
            props ??= new List<IPropertySymbol>();
            props.AddRange(entitySymbol.GetMembers().OfType<IPropertySymbol>().Where(a => !a.IsReadOnly));

            //recursive base types
            if (entitySymbol.BaseType != null)
            {
                GetAllProperties(entitySymbol.BaseType, props);
            }

            return props.ToArray();
        }

        internal static IPropertySymbol[] GetFlagProperties(INamedTypeSymbol entitySymbol)
        {
            return GetAllProperties(entitySymbol)
                .Where(a =>
                        IsCollection(a.Type) ||
                        IsEntity(a.Type))
                .OrderBy(a => a.Name)
                .ToArray();
        }

        internal static bool IsEntity(ITypeSymbol entitySymbol)
        {
            return IsTypeOrImplementsInterface(entitySymbol, "IEntity");

        }

        internal static bool IsCollection(ITypeSymbol entitySymbol)
        {
            return IsTypeOrImplementsInterface(entitySymbol, typeof(IList)) ||
                   IsTypeOrImplementsInterface(entitySymbol, $"ICollection");

        }

        internal static List<IPropertySymbol> GetMappedProperties(INamedTypeSymbol entitySymbol)
        {
            var props = GetAllProperties(entitySymbol).Where(a =>
                a.Name != "Id" &&
                a.Type is INamedTypeSymbol symbol &&
                !IsCollection(symbol) &&
                !IsEntity(symbol) &&
                symbol.AllInterfaces.All(b => b.Name != "IEntity")).ToList();

            return props;
        }

        internal static IPropertySymbol[] GetOneToOneProperties(INamedTypeSymbol entitySymbol)
        {
            return GetAllProperties(entitySymbol).Where(a =>
                a.Type is INamedTypeSymbol { IsGenericType: false } symbol &&
                symbol.AllInterfaces.Any(b => b.Name == "IEntity")).ToArray();
        }


        internal static bool IsTypeOrImplementsInterface(ITypeSymbol typeSymbol, Type targetType)
        {
            return targetType.FullName != null && IsTypeOrImplementsInterface(typeSymbol, targetType.FullName);
        }

        internal static bool IsTypeOrImplementsInterface(ITypeSymbol typeSymbol, string targetType)
        {
            if (typeSymbol.ToDisplayString().Split('<').First().EndsWith(targetType))
            {
                return true;
            }

            foreach (var iface in typeSymbol.AllInterfaces)
            {
                if (iface.ToDisplayString().Split('<').First().EndsWith(targetType))
                {
                    return true;
                }
            }

            return false;
        }

        internal static IPropertySymbol[] GetOneToManyProperties(INamedTypeSymbol entitySymbol)
        {
            return GetAllProperties(entitySymbol).Where(a =>
                a.Type is INamedTypeSymbol symbol &&
                IsCollection(symbol) &&
                symbol.TypeArguments.All(b => b.AllInterfaces.Any(c => c.Name == "IEntity")))
                .Where(a =>
                    GetAllProperties(((a.Type as INamedTypeSymbol)?.TypeArguments[0] as INamedTypeSymbol)!)
                        .Any(b => b.Name == $"{entitySymbol.Name}Id")
                )
                .ToArray();

        }

        internal static IPropertySymbol[] GetManyToManyProperties(INamedTypeSymbol entitySymbol)
        {
            return GetAllProperties(entitySymbol).Where(a =>
                    a.Type is INamedTypeSymbol symbol &&
                    IsCollection(symbol) &&
                    symbol.TypeArguments.Count() == 1 &&
                    symbol.TypeArguments.All(b => b.AllInterfaces.Any(c => c.Name == "IEntity"))).Where(a =>
                    GetAllProperties(((a.Type as INamedTypeSymbol)?.TypeArguments[0] as INamedTypeSymbol)!)  //get all properties of the collection type
                        .Any(b => b.Type is       //make sure the property type is of entitySymbol type
                             INamedTypeSymbol symbol &&
                             IsCollection(symbol) &&
                             symbol.TypeArguments.Count() == 1 &&
                             symbol.TypeArguments.All(c => SymbolEqualityComparer.Default.Equals(c,entitySymbol))))
                .ToArray();

        }

        internal static string HandleWhenNull(IPropertySymbol prop)
        {
            if (prop.Type.NullableAnnotation == NullableAnnotation.Annotated ||
                prop.Type is INamedTypeSymbol { IsGenericType: true, OriginalDefinition.SpecialType: SpecialType.System_Nullable_T })
            {
                return "?? (object)DBNull.Value";

            }

            return string.Empty;
        }

    
    }
}
