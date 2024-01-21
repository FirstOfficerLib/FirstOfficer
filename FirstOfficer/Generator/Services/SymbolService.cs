using System.Collections;
using System.Collections.Concurrent;
using FirstOfficer.Extensions;
using Microsoft.CodeAnalysis;

namespace FirstOfficer.Generator.Services
{
    internal static class SymbolService
    {
        private static ConcurrentDictionary<string, IPropertySymbol[]> _allProperties = new();

        internal static IPropertySymbol[] GetAllProperties(INamedTypeSymbol entitySymbol)
        {
            //caching results
            var key = entitySymbol.FullName();
            if (_allProperties.TryGetValue(key, out var properties))
            {
                return properties;
            }

            var symbol = entitySymbol;
            var props = new List<IPropertySymbol>();
            do
            {
                props.AddRange(symbol.GetMembers().OfType<IPropertySymbol>().Where(a => !a.IsReadOnly));
                symbol = symbol.BaseType;
            } while (symbol?.BaseType != null);

            var rtn = props.ToArray();

            _allProperties.TryAdd(key, rtn);

            return rtn;
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
            if (typeSymbol == null)
            {
                return false;
            }

            if (typeSymbol.ToDisplayString().Split('<').First().EndsWith(targetType))
            {
                return true;
            }

            foreach (var interfaceSymbol in typeSymbol.AllInterfaces)
            {
                if (interfaceSymbol.ToDisplayString().Split('<').First().EndsWith(targetType))
                {
                    return true;
                }
            }

            return false;
        }


        public static bool IsNullable(ITypeSymbol symbol)
        {
            return symbol.NullableAnnotation == NullableAnnotation.Annotated;
        }
    }
}
