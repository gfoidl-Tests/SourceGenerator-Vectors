// (c) gfoidl, all rights reserved

using Microsoft.CodeAnalysis;

namespace Generator.Models;
internal record IndexOfAnyMethod(ContainingTypeInfo Type, MethodInfo Method)
{
    public static IndexOfAnyMethod Create(IMethodSymbol methodSymbol, string setChars, bool findAnyExcept)
    {
        ContainingTypeInfo typeInfo = ContainingTypeInfo.Create(methodSymbol);
        MethodInfo methodInfo       = MethodInfo.Create(methodSymbol, setChars, findAnyExcept);

        return new IndexOfAnyMethod(typeInfo, methodInfo);
    }
}
