using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace FirstOfficer.Generator.Helpers
{
    internal static class SyntaxHelper
    {
        internal static string FormatCode(string output)
        {
            AdhocWorkspace workspace = new AdhocWorkspace();
            var solution = workspace.CurrentSolution;

            // Adjust the options for the solution
            var newOptions = solution.Options
                .WithChangedOption(FormattingOptions.IndentationSize, LanguageNames.CSharp, 4)
                .WithChangedOption(FormattingOptions.UseTabs, LanguageNames.CSharp, false);

            // Apply the updated options to the solution
            solution = solution.WithOptions(newOptions);

            // Add a new project to the solution
            var projectId = ProjectId.CreateNewId();
            var projectInfo = ProjectInfo.Create(
                projectId,
                VersionStamp.Default,
                "AdhocProject",
                "AdhocAssembly",
                LanguageNames.CSharp,
                metadataReferences: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) }
            );

            solution = solution.AddProject(projectInfo);

            // Add the unformatted code to a new document within the project
            var documentId = DocumentId.CreateNewId(projectId);
            solution = solution.AddDocument(documentId, "AdhocDocument.cs", output);

            var document = solution.GetDocument(documentId);

            // Format the document
            var formattedRoot = Formatter.Format(document.GetSyntaxRootAsync().Result, workspace);

            // If you wish to retrieve the formatted code as a string:
            string formattedCode = formattedRoot.ToFullString();
            return formattedCode;
        }
    }
}
