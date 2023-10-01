using System.Globalization;
using System.Text.RegularExpressions;

namespace FirstOfficer.Core.Extensions
{
    public static class StringExtensions
    {
        public static string ToSnakeCase(this string str)
        {
            if (string.IsNullOrEmpty(str)) return str;

            // Convert PascalCase to snake_case
            str = Regex.Replace(str, @"(\w)([A-Z])", "$1_$2");

            return str.ToLower();
        }

        public static string ToPascalCase(this string str)
        {
            if (string.IsNullOrEmpty(str)) return str;

            TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;

            return string.Join(string.Empty, str.Split('_').Select(s => textInfo.ToTitleCase(s)));
        }
    }
}
