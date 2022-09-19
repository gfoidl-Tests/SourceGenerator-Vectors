// (c) gfoidl, all rights reserved

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace Generator;

public partial class IndexOfAnyGenerator
{
    private static bool IsSyntaxTargetForGeneration(SyntaxNode node, CancellationToken _)
        // We don't have a semantic model here, so the best we can do is say whether there are any attributes.
        => node is MethodDeclarationSyntax { AttributeLists.Count: > 0 };
    //-------------------------------------------------------------------------
    /// <summary>
    /// Returns <c>null</c> if nothing to do, <see cref="Diagnostic"/> if there's an error to report,
    /// or <see cref="IndexOfAnyMethod"/> if the type was analyzed successfully.
    /// </summary>
    private static object? GetSemanticTargetForGeneration(GeneratorSyntaxContext context, CancellationToken _)
    {
        SemanticModel semanticModel                     = context.SemanticModel;
        MethodDeclarationSyntax methodDeclarationSyntax = (MethodDeclarationSyntax)context.Node;

        Compilation compilation                     = semanticModel.Compilation;
        INamedTypeSymbol? indexOfAnyAttributeSymbol = compilation.GetBestTypeByMetadataName(GeneratedIndexOfAnyAttributeName);

        if (indexOfAnyAttributeSymbol is null)
        {
            // Required types aren't available
            return null;
        }

        if (semanticModel.GetDeclaredSymbol(methodDeclarationSyntax) is not { } methodSymbol) return null;
        if (!IsMethodDeclarationPartial(methodSymbol))                                        return null;
        if (!ReturnsInt32(methodSymbol))                                                      return null;

        object? argumentTupleOrDiagnostic = GetIndexOfAnyOptionsOrDiagnostic(methodSymbol, methodDeclarationSyntax);

        if (argumentTupleOrDiagnostic is IndexOfAnyOptions options)
        {
            if (ValidateSetChars(options.SetChars, methodSymbol, methodDeclarationSyntax, out Diagnostic? diagnostic))
            {
                return IndexOfAnyMethod.Create(methodSymbol, options);
            }

            argumentTupleOrDiagnostic = diagnostic;
        }

        // Diagnostic or null (which is filtered out later)
        return argumentTupleOrDiagnostic;
    }
    //-------------------------------------------------------------------------
    private static bool IsMethodDeclarationPartial(IMethodSymbol methodSymbol)
        => methodSymbol.IsPartialDefinition;
    //-------------------------------------------------------------------------
    private static bool ReturnsInt32(IMethodSymbol methodSymbol)
        => methodSymbol.ReturnType?.SpecialType == SpecialType.System_Int32;
    //-------------------------------------------------------------------------
    private static object? GetIndexOfAnyOptionsOrDiagnostic(IMethodSymbol methodSymbol, CSharpSyntaxNode syntaxNode)
    {
        foreach (AttributeData attribute in methodSymbol.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == GeneratedIndexOfAnyAttributeName)
            {
                ImmutableArray<TypedConstant> ctorArgs = attribute.ConstructorArguments;

                if (ctorArgs.Length != 1)
                {
                    return Diagnostic.Create(DiagnosticDescriptors.AttributeArgumentCountMismatch, syntaxNode.GetLocation());
                }

                if (ctorArgs[0].Value is string setChars)
                {
                    if (attribute.NamedArguments.Length == 0)
                    {
                        return new IndexOfAnyOptions(setChars, false);
                    }

                    if (attribute.NamedArguments[0].Key == "FindAnyExcept")
                    {
                        bool findAnyExcept = (bool)(attribute.NamedArguments[0].Value.Value ?? false);
                        return new IndexOfAnyOptions(setChars, findAnyExcept);
                    }
                }
            }
        }

        return null;
    }
    //-------------------------------------------------------------------------
    private static bool ValidateSetChars(
        string           setChars,
        IMethodSymbol    methodSymbol,
        CSharpSyntaxNode syntaxNode,
        [NotNullWhen(false)] out Diagnostic? diagnostic)
    {
        // Further analysis could be done here. In case of failure, report a diagnostic.

        if (string.IsNullOrWhiteSpace(setChars))
        {
            diagnostic = Diagnostic.Create(DiagnosticDescriptors.SetCharsIsNullOrEmpty, syntaxNode.GetLocation());
            return false;
        }

        if (!SetCharsValidator.IsSetCharsValid(setChars.AsSpan()))
        {
            diagnostic = Diagnostic.Create(DiagnosticDescriptors.SetCharsNotValid, syntaxNode.GetLocation());
            return false;
        }

        if (methodSymbol.Parameters.Length != 1)
        {
            diagnostic = Diagnostic.Create(DiagnosticDescriptors.MethodHasMoreThanOneArgument, syntaxNode.GetLocation());
            return false;
        }

        if (methodSymbol.Parameters[0].ToDisplayString() != "System.ReadOnlySpan<char>")
        {
            diagnostic = Diagnostic.Create(DiagnosticDescriptors.ArgumentHasWrongType, syntaxNode.GetLocation());
            return false;
        }

        diagnostic = null;
        return true;
    }
}
