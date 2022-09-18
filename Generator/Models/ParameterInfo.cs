// (c) gfoidl, all rights reserved

using Microsoft.CodeAnalysis;

namespace Generator.Models;

internal readonly record struct ParameterInfo(string Name, ITypeSymbol Type);
