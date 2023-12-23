using Microsoft.CodeAnalysis;

namespace FirstOfficer.Generator.Mapper
{
    internal class PropertyMapping
    {
        public IPropertySymbol SourceSymbol { get; set; } = null!;
        public IPropertySymbol TargetSymbol { get; set; } = null!;
    }
}
