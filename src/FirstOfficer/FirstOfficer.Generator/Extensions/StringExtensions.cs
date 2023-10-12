using System.Globalization;
using System.Text.RegularExpressions;

namespace FirstOfficer.Generator.Extensions
{
    public static class StringExtensions
    {
        public static string ToSnakeCase(this string str)
        {
            if (string.IsNullOrEmpty(str)) return str;

            // Convert PascalCase to snake_case
            string result = Regex.Replace(str, @"(?<=[a-z])([A-Z])", "_$1");
            return result.ToLower();
        }

        public static string ToPascalCase(this string str)
        {
            if (string.IsNullOrEmpty(str)) return str;

            TextInfo textInfo = CultureInfo.InvariantCulture.TextInfo;

            return string.Join(string.Empty, str.Split('_').Select(s => textInfo.ToTitleCase(s)));
        }
    }
}
