﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutomaticInterface
{
    /// <summary>
    /// Source: https://github.com/dominikjeske/Samples/blob/main/SourceGenerators/HomeCenter.SourceGenerators/Extensions/RoslynExtensions.cs
    /// </summary>
    public static class RoslynExtensions
    {
        private static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(this ITypeSymbol type)
        {
            var current = type;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol type)
        {
            return type.GetBaseTypesAndThis().SelectMany(n => n.GetMembers());
        }

        public static string GetClassName(this ClassDeclarationSyntax proxy)
        {
            return proxy.Identifier.Text;
        }

        /// <summary>
        /// Thanks to https://www.codeproject.com/Articles/871704/Roslyn-Code-Analysis-in-Easy-Samples-Part-2
        /// </summary>
        public static string GetWhereStatement(
            this ITypeParameterSymbol typeParameterSymbol,
            SymbolDisplayFormat typeDisplayFormat
        )
        {
            var result = $"where {typeParameterSymbol.Name} : ";

            var constraints = "";

            var isFirstConstraint = true;

            if (typeParameterSymbol.HasReferenceTypeConstraint)
            {
                constraints += "class";
                isFirstConstraint = false;
            }

            if (typeParameterSymbol.HasValueTypeConstraint)
            {
                constraints += "struct";
                isFirstConstraint = false;
            }

            if (typeParameterSymbol.HasConstructorConstraint)
            {
                constraints += "new()";
                isFirstConstraint = false;
            }

            if (typeParameterSymbol.HasNotNullConstraint)
            {
                constraints += "notnull";
                isFirstConstraint = false;
            }

            foreach (var constraintType in typeParameterSymbol.ConstraintTypes)
            {
                // if not first constraint prepend with comma
                if (!isFirstConstraint)
                {
                    constraints += ", ";
                }
                else
                {
                    isFirstConstraint = false;
                }

                constraints += constraintType.GetFullTypeString(typeDisplayFormat);
            }

            if (string.IsNullOrEmpty(constraints))
            {
                return "";
            }

            result += constraints;

            return result;
        }

        private static string GetFullTypeString(
            this ISymbol type,
            SymbolDisplayFormat typeDisplayFormat
        )
        {
            return type.ToDisplayString(typeDisplayFormat)
                + type.GetTypeArgsStr(
                    typeDisplayFormat,
                    symbol => ((INamedTypeSymbol)symbol).TypeArguments
                );
        }

        private static string GetTypeArgsStr(
            this ISymbol symbol,
            SymbolDisplayFormat typeDisplayFormat,
            Func<ISymbol, ImmutableArray<ITypeSymbol>> typeArgGetter
        )
        {
            var typeArgs = typeArgGetter(symbol);

            if (!typeArgs.Any())
                return string.Empty;

            var stringsToAdd = new List<string>();
            foreach (var arg in typeArgs)
            {
                string strToAdd;

                if (arg is ITypeParameterSymbol typeParameterSymbol)
                {
                    // this is a generic argument
                    strToAdd = typeParameterSymbol.ToDisplayString(typeDisplayFormat);
                }
                else
                {
                    // this is a generic argument value.
                    var namedTypeSymbol = arg as INamedTypeSymbol;
                    strToAdd = namedTypeSymbol!.GetFullTypeString(typeDisplayFormat);
                }

                stringsToAdd.Add(strToAdd);
            }

            var result = $"<{string.Join(", ", stringsToAdd)}>";

            return result;
        }
    }
}
