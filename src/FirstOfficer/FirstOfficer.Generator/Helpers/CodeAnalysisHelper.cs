using Microsoft.CodeAnalysis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace FirstOfficer.Generator.Helpers
{
    internal static class CodeAnalysisHelper
    {
        internal static IEnumerable<IPropertySymbol> GetAllProperties(INamedTypeSymbol entitySymbol,
            List<IPropertySymbol>? props = null!)
        {
            props ??= new List<IPropertySymbol>();
            props.AddRange(entitySymbol.GetMembers().OfType<IPropertySymbol>().Where(a => !a.IsReadOnly));

            //recursive base types
            if (entitySymbol.BaseType != null)
            {
                GetAllProperties(entitySymbol.BaseType, props);
            }

            return props;
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

        internal static IEnumerable<IPropertySymbol> GetMappedProperties(INamedTypeSymbol entitySymbol)
        {
            var props = GetAllProperties(entitySymbol).Where(a =>
                a.Name != "Id" &&
                a.Type is INamedTypeSymbol { IsGenericType: false } symbol &&
                symbol.AllInterfaces.All(b => b.Name != "IEntity")).ToList();

            return props;
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
    }
}
