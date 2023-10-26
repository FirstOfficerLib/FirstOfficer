using FirstOfficer.Generator.Helpers;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using FirstOfficer.Generator.Mapper;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace FirstOfficer.Generator.Services
{
    internal static class MapperSymbolService
    {

        internal static List<EntityOrmMapping> GetEntityDtoMappings(Microsoft.CodeAnalysis.Compilation compilation)
        {
            var rtn = new List<EntityOrmMapping>();

            var entities = compilation.SyntaxTrees.SelectMany(a=> a.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
                .Where(a => OrmSymbolService.IsEntity(compilation.GetSemanticModel(a.SyntaxTree).GetDeclaredSymbol(a)))
                .Select(a => compilation.GetSemanticModel(a.SyntaxTree).GetDeclaredSymbol(a)).ToList();

            var dtos = compilation.SyntaxTrees.SelectMany(a => a.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
                .Where(a => IsDto(compilation.GetSemanticModel(a.SyntaxTree).GetDeclaredSymbol(a)))
                .Select(a => compilation.GetSemanticModel(a.SyntaxTree).GetDeclaredSymbol(a)).ToList();

            foreach (var dto in dtos.Where(a => a is not null && entities
                         .Where(a => a is not null)
                         .Select(b => b!.Name).ToArray().Contains(a.Name)))
            {
                var entity = entities.First(a => a!.Name == dto!.Name);
                if (entity != null && dto != null)
                {
                    rtn.Add(new EntityOrmMapping
                    {
                        EntitySymbol = entity,
                        DtoSymbol = dto!,
                        EntityToDtoPropertyMappings = GetPropertyMappings(entity, dto),
                        DtoToEntityPropertyMappings = GetPropertyMappings(entity, dto),
                    });
                }
            }


            return rtn;
        }

        private static List<PropertyMapping> GetPropertyMappings(INamedTypeSymbol source, INamedTypeSymbol target)
        {
            var rtn = new List<PropertyMapping>();
            var sourceProps = SymbolService.GetAllProperties(source);
            var targetProps = SymbolService.GetAllProperties(target);

            foreach (var sourceProp in sourceProps)
            {
                var targetProp = targetProps.FirstOrDefault(a => a.Name == sourceProp.Name);
                if (targetProp != null)
                {
                    rtn.Add(new PropertyMapping
                    {
                        SourceSymbol = sourceProp,
                        TargetSymbol = targetProp
                    });
                }
            }

            return rtn;

        }

        internal static bool IsDto(INamedTypeSymbol? symbol)
        {
            return SymbolService.IsTypeOrImplementsInterface(symbol, "IDto");
        }


    }
}
