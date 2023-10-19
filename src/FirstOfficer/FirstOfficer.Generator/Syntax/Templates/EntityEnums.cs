using System.Collections;
using System.Text;
using FirstOfficer.Generator.Extensions;
using FirstOfficer.Generator.Services;
using Microsoft.CodeAnalysis;

namespace FirstOfficer.Generator.Syntax.Templates
{
    internal static class EntityEnums
    {

        internal static string GetTemplate(INamedTypeSymbol entitySymbol)
        {
            var valueProperties = OrmSymbolService.GetFlagProperties(entitySymbol);

            if (valueProperties.Length == 0)
            {
                return string.Empty;
            }

            var values = new StringBuilder();
            var i = 1;
            foreach (var propertySymbol in valueProperties)
            {
                values.AppendLine($"{propertySymbol.Name} = {i},");
                i *= 2;
            }

            var rtn = $@" 
                [Flags] 
                public enum Includes
                {{
                    None = 0,
                    {values}
                    All = {i - 1}
                }}
";
            return rtn;
        }
        
    }
}
