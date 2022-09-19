// (c) gfoidl, all rights reserved

using Microsoft.CodeAnalysis;

namespace Generator.Models;

internal record IndexOfAnyMethod(ContainingTypeInfo Type, MethodInfo Method)
{
    public static IndexOfAnyMethod Create(IMethodSymbol methodSymbol, IndexOfAnyOptions options)
    {
        ContainingTypeInfo typeInfo = ContainingTypeInfo.Create(methodSymbol);
        MethodInfo methodInfo       = MethodInfo.Create(methodSymbol, options);

        return new IndexOfAnyMethod(typeInfo, methodInfo);
    }
}
