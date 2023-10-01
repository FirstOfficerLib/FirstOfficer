using FirstOfficer.Generator.Extensions;
using FirstOfficer.Generator.Helpers;
using Microsoft.CodeAnalysis;

namespace FirstOfficer.Generator.Syntax.Templates
{
    internal static class EntityChecksum
    {

        internal static string GetTemplate(INamedTypeSymbol entitySymbol)
        {
            var valueProperties = CodeAnalysisHelper.GetMappedProperties(entitySymbol)
                .Where(a=> a.Name != "Checksum")
                .OrderBy(a=> a.Name)
                .ToArray();

            var rtn = $@"
                public static string Checksum(this {entitySymbol.FullName()} entity)
                {{
                    var sb = new StringBuilder();
                    sb.Append(entity.Id);
                    {string.Join("\r\n", valueProperties.Select(GetAppend))}
                    byte[] hashBytes = System.Security.Cryptography.SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
                    return BitConverter.ToString(hashBytes).Replace(""-"", """");
                }}
";
            return rtn;
        }

        private static string GetAppend(IPropertySymbol propertySymbol)
        {
            if (((INamedTypeSymbol)propertySymbol.Type).FullName() == typeof(decimal).FullName ||
                ((INamedTypeSymbol)propertySymbol.Type).FullName() == typeof(decimal?).FullName)
            {
                return $@"sb.Append(entity.{propertySymbol.Name}.ToString(""F16""));";

            }

            if (((INamedTypeSymbol)propertySymbol.Type).FullName() == typeof(DateTime).FullName ||
                ((INamedTypeSymbol)propertySymbol.Type).FullName() == typeof(DateTime?).FullName)
            {
                return $@"sb.Append(entity.{propertySymbol.Name}.ToString(""yyyy-MM-ddTHH:mm:ss.fff""));";
            }

            return $@"sb.Append(entity.{propertySymbol.Name});";
        }
    }
}
