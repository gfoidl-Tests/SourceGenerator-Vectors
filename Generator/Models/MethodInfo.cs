// (c) gfoidl, all rights reserved

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Generator.Models;

internal record MethodInfo(
    string            Name,
    Accessibility     Accessibility,
    bool              IsStatic,
    IndexOfAnyOptions IndexOfAnyOptions,
    ImmutableArray<ParameterInfo> Parameters)
{
    public static MethodInfo Create(IMethodSymbol methodSymbol, IndexOfAnyOptions indexOfAnyOptions)
    {
        ImmutableArray<ParameterInfo>.Builder builder = ImmutableArray.CreateBuilder<ParameterInfo>(methodSymbol.Parameters.Length);

        foreach (IParameterSymbol parameter in methodSymbol.Parameters)
        {
            string parameterName   = parameter.Name;
            ITypeSymbol typeSymbol = parameter.Type;

            builder.Add(new ParameterInfo(parameterName, typeSymbol));
        }

        return new MethodInfo(methodSymbol.Name, methodSymbol.DeclaredAccessibility, methodSymbol.IsStatic, indexOfAnyOptions, builder.ToImmutableArray());
    }
}
