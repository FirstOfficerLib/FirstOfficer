using Microsoft.CodeAnalysis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using FirstOfficer.Generator.Extensions;

namespace FirstOfficer.Generator.Services
{
    internal static class SymbolService
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

        internal static bool IsCollection(ITypeSymbol? entitySymbol)
        {
            return IsTypeOrImplementsInterface(entitySymbol, typeof(IList)) ||
                   IsTypeOrImplementsInterface(entitySymbol, $"ICollection");

        }
        internal static bool IsTypeOrImplementsInterface(ITypeSymbol? typeSymbol, Type targetType)
        {
            return targetType.FullName != null && IsTypeOrImplementsInterface(typeSymbol, targetType.FullName);
        }

        internal static bool IsTypeOrImplementsInterface(ITypeSymbol? typeSymbol, string targetType)
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
