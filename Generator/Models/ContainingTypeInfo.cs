// (c) gfoidl, all rights reserved

using Microsoft.CodeAnalysis;

namespace Generator.Models;

internal readonly record struct ContainingTypeInfo(string? Namespace, string Name, string TypeKind)
{
    public static ContainingTypeInfo Create(IMethodSymbol methodSymbol)
    {
        INamedTypeSymbol containingType = methodSymbol.ContainingType;
        string? ns                      = GetNamespace(containingType);
        string typeKind                 = GetTypeKind(containingType);

        return new ContainingTypeInfo(ns, containingType.Name, typeKind);
    }
    //-------------------------------------------------------------------------
    private static string? GetNamespace(INamedTypeSymbol namedTypeSymbol)
    {
        INamespaceSymbol containingNamespace = namedTypeSymbol.ContainingNamespace;
        return string.IsNullOrEmpty(containingNamespace.Name) ? null : containingNamespace.ToDisplayString();
    }
    //-------------------------------------------------------------------------
    private static string GetTypeKind(INamedTypeSymbol containingType)
        => containingType.TypeKind switch
        {
            Microsoft.CodeAnalysis.TypeKind.Struct    => "struct",
            Microsoft.CodeAnalysis.TypeKind.Interface => "interface",
            _ => "class"
        };
}
