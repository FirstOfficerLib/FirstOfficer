using Microsoft.CodeAnalysis;

namespace FirstOfficer.Generator.Diagnostics
{
    public static class DiagnosticDescriptors
    {

        //ERRORS
        public static DiagnosticDescriptor LanguageVersionNotSupported(DiagnosticCategories diagnosticCategories)
        {
            return new DiagnosticDescriptor(
                "SHIP000001",
                "The used C# language version is not supported by First Officer, at least C# 9.0 is required",
                "First Officer does not support the C# language version {0} but requires at C# least version {1}",
                diagnosticCategories.ToString(),
                DiagnosticSeverity.Error,
                true);
        }








        //INFO
    }
}
