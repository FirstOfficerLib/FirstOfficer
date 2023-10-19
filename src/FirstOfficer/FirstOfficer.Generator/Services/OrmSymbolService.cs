using Microsoft.CodeAnalysis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using FirstOfficer.Generator.Extensions;

namespace FirstOfficer.Generator.Services
{
    internal static class OrmSymbolService
    {


        internal static IPropertySymbol[] GetQueryableProperties(INamedTypeSymbol entitySymbol)
        {
            return SymbolService.GetAllProperties(entitySymbol)
                .Where(a => a.GetAttributes().Any(b => b.AttributeClass?.Name == "QueryableAttribute")).ToArray();
        }

        internal static IPropertySymbol[] GetOrderByProperties(INamedTypeSymbol entitySymbol)
        {
            return SymbolService.GetAllProperties(entitySymbol)
                .Where(a => a.GetAttributes().Any(b => b.AttributeClass?.Name == "OrderByAttribute")).ToArray();
        }

        internal static IPropertySymbol[] GetFlagProperties(INamedTypeSymbol entitySymbol)
        {
            return SymbolService.GetAllProperties(entitySymbol)
                .Where(a =>
                    SymbolService.IsCollection(a.Type) ||
                        IsEntity(a.Type))
                .OrderBy(a => a.Name)
                .ToArray();
        }

        internal static bool IsEntity(ITypeSymbol? entitySymbol)
        {
            if (entitySymbol == null)
            {
                return false;
            }

            return SymbolService.IsTypeOrImplementsInterface(entitySymbol, "IEntity");

        }


        internal static List<IPropertySymbol> GetMappedProperties(INamedTypeSymbol entitySymbol)
        {
            var props = SymbolService.GetAllProperties(entitySymbol).Where(a =>
                a.Name != "Id" &&
                a.Type is INamedTypeSymbol symbol &&
                !SymbolService.IsCollection(symbol) &&
                !IsEntity(symbol) &&
                symbol.AllInterfaces.All(b => !IsEntity(b))).ToList();

            return props;
        }

        internal static IPropertySymbol[] GetOneToOneProperties(INamedTypeSymbol entitySymbol)
        {
            return SymbolService.GetAllProperties(entitySymbol).Where(a =>
                a.Type is INamedTypeSymbol { IsGenericType: false } symbol &&
                symbol.AllInterfaces.Any(IsEntity)).ToArray();
        }



        internal static IPropertySymbol[] GetOneToManyProperties(INamedTypeSymbol entitySymbol)
        {
            return SymbolService.GetAllProperties(entitySymbol).Where(a =>
                a.Type is INamedTypeSymbol symbol &&
                SymbolService.IsCollection(symbol) &&
                symbol.TypeArguments.All(b => b.AllInterfaces.Any(IsEntity)))
                .Where(a =>
                    SymbolService.GetAllProperties(((a.Type as INamedTypeSymbol)?.TypeArguments[0] as INamedTypeSymbol)!)
                        .Any(b => b.Name == $"{entitySymbol.Name}Id")
                )
                .ToArray();

        }

        internal static IPropertySymbol[] GetManyToManyProperties(INamedTypeSymbol entitySymbol)
        {
            return SymbolService.GetAllProperties(entitySymbol).Where(a =>
                    a.Type is INamedTypeSymbol symbol &&
                    SymbolService.IsCollection(symbol) &&
                    symbol.TypeArguments.Count() == 1 &&
                    symbol.TypeArguments.All(b => b.AllInterfaces.Any(IsEntity))).Where(a =>
                    SymbolService.GetAllProperties(((a.Type as INamedTypeSymbol)?.TypeArguments[0] as INamedTypeSymbol)!)  //get all properties of the collection type
                        .Any(b => b.Type is       //make sure the property type is of entitySymbol type
                             INamedTypeSymbol symbol &&
                                  SymbolService.IsCollection(symbol) &&
                             symbol.TypeArguments.Count() == 1 &&
                             symbol.TypeArguments.All(c => SymbolEqualityComparer.Default.Equals(c, entitySymbol))))
                .ToArray();

        }

        internal static string HandleWhenNull(IPropertySymbol prop)
        {
            if (prop.Type.NullableAnnotation == NullableAnnotation.Annotated ||
                prop.Type.Name.ToLower() == "string" ||
                prop.Type is INamedTypeSymbol { IsGenericType: true, OriginalDefinition.SpecialType: SpecialType.System_Nullable_T })
            {
                if (prop.Type.Name.ToLower() == "nullable" &&
                    !((INamedTypeSymbol)prop.Type).TypeArguments.IsDefaultOrEmpty &&
                    ((INamedTypeSymbol)prop.Type).TypeArguments[0].Name.ToLower() == "int64")
                {
                    return $" NpgsqlTypes.NpgsqlDbType.Bigint , entity.{prop.Name} ?? (object)DBNull.Value";
                }
                if (prop.Type.Name.ToLower() == "nullable" &&
                    !((INamedTypeSymbol)prop.Type).TypeArguments.IsDefaultOrEmpty &&
                    ((INamedTypeSymbol)prop.Type).TypeArguments[0].Name.ToLower() == "int32")
                {
                    return $" NpgsqlTypes.NpgsqlDbType.Integer , entity.{prop.Name} ?? (object)DBNull.Value";
                }

                return $" entity.{prop.Name} ?? (object)DBNull.Value";

            }

            return $" entity.{prop.Name} ";
        }


    }
}
