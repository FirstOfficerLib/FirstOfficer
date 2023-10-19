using System;
using System.Collections.Generic;
using System.Text;

namespace FirstOfficer.Generator.Compilation
{
    internal sealed class CompilationContext
    {
        public Microsoft.CodeAnalysis.Compilation Compilation { get; }
        public FileNameBuilder FileNameBuilder { get; }

        internal CompilationContext(Microsoft.CodeAnalysis.Compilation compilation, FileNameBuilder fileNameBuilder)
        {
            Compilation = compilation;
            FileNameBuilder = fileNameBuilder;
        }
    }
}
