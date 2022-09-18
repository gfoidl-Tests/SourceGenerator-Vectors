// (c) gfoidl, all rights reserved

using Microsoft.CodeAnalysis;

namespace Generator;

internal static class DiagnosticDescriptors
{
    private const string Category = "GeneratedIndexOfAny";
    //-------------------------------------------------------------------------
    public static DiagnosticDescriptor AttributeArgumentCountMismatch { get; } = new DiagnosticDescriptor(
       id                : "GIOA001",
       title             : "Argument count mismatch",
       messageFormat     : $"The {IndexOfAnyGenerator.GeneratedIndexOfAnyAttributeName} expects exactly 1 argument for the setChars.",
       category          : Category,
       DiagnosticSeverity.Error,
       isEnabledByDefault: true);
    //-------------------------------------------------------------------------
    public static DiagnosticDescriptor SetCharsIsNullOrEmpty { get; } = new DiagnosticDescriptor(
        id                : "GIOA002",
        title             : "SetChars is null or empty",
        messageFormat     : "The given SetChars must not be null or an empty string.",
        category          : Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    //-------------------------------------------------------------------------
    public static DiagnosticDescriptor SetCharsNotValid { get; } = new DiagnosticDescriptor(
        id                : "GIOA003",
        title             : "SetChars is not valid",
        messageFormat     : "The given SetChars is not valid ... TODO: better message",
        category          : Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    //-------------------------------------------------------------------------
    public static DiagnosticDescriptor MethodHasMoreThanOneArgument { get; } = new DiagnosticDescriptor(
        id                : "GIOA004",
        title             : "Method has more than one argument",
        messageFormat     : "The method must have exactly one argument",
        category          : Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    //-------------------------------------------------------------------------
    public static DiagnosticDescriptor ArgumentHasWrongType { get; } = new DiagnosticDescriptor(
        id                : "GIOA005",
        title             : "Argument has wrong type",
        messageFormat     : "The type of the argument must be ReadOnlySpan<char>",
        category          : Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
