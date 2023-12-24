using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

namespace FirstOfficer.Generator.Diagnostics
{
    public static class CompilationDiagnostics
    {
        public static IEnumerable<Diagnostic> BuildCompilationDiagnostics(Microsoft.CodeAnalysis.Compilation compilationDiagnostics, DiagnosticCategories diagnosticCategories)
        {
            if (compilationDiagnostics is CSharpCompilation { LanguageVersion: < LanguageVersion.CSharp9 } cSharpCompilation)
            {
                yield return Diagnostic.Create(
                    DiagnosticDescriptors.LanguageVersionNotSupported(diagnosticCategories),
                    null,
                    cSharpCompilation.LanguageVersion.ToDisplayString(),
                    LanguageVersion.CSharp9.ToDisplayString()
                );
            }
        }
    }
}
