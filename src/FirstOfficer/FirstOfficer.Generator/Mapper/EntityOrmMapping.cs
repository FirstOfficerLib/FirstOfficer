﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FirstOfficer.Generator.Mapper
{
    internal class EntityOrmMapping
    {
        public INamedTypeSymbol EntitySymbol { get; set; } = null!;
        public INamedTypeSymbol DtoSymbol { get; set; } = null!;

        public List<PropertyMapping> EntityToDtoPropertyMappings { get; set; } = new();
        public List<PropertyMapping> DtoToEntityPropertyMappings { get; set; } = new();

        public override string ToString()
        {
            return $"{EntitySymbol.Name}{DtoSymbol.Name}";
        }
    }
}