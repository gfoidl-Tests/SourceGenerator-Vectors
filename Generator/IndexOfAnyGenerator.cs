// (c) gfoidl, all rights reserved

using System.Collections.Immutable;
using System.Diagnostics;
using Generator.Emitter;
using Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Generator;

[Generator(LanguageNames.CSharp)]
public partial class IndexOfAnyGenerator : IIncrementalGenerator
{
    internal const string GeneratedIndexOfAnyAttributeName = "GeneratedIndexOfAnyAttribute";

    // Name is lowercased
    private const string GeneratedIndexOfAnyDebuggerHiddenDisabledMetadata = "build_property.generatedindexofanydebuggerhiddendisabled";
    //-------------------------------------------------------------------------
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //System.Diagnostics.Debugger.Launch();

        context.RegisterPostInitializationOutput(AddAttribute);

        IncrementalValueProvider<ImmutableArray<object?>> codeOrDiagnostics = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _)    => IsSyntaxTargetForGeneration(node, _),
                transform: static (context, _) => GetSemanticTargetForGeneration(context, _)
            )
            .Where(indexOfAnyMethod => indexOfAnyMethod is not null)
            // This step isn't necessary. One could further transform the so-far collected data.
            // Here it's just sanitation.
            .Select(static (state, _) =>
            {
                if (state is not IndexOfAnyMethod indexOfAnyMethod)
                {
                    Debug.Assert(state is Diagnostic);
                    return state;
                }

                return state;
            })
            .Collect();

        // To avoid invalidating every generator's output when anything from the compilation
        // changes, we extract from it only things we care about: whether
        // * unsafe code is allowed
        // * file scoped namespaces
        // are allowed, and only that information is then fed into RegisterSourceOutput along with all
        // of the cached generated data from each named format.
        IncrementalValueProvider<(bool AllowUnsafe, string? AssemblyName)> compilationData = context.CompilationProvider
            .Select(static (c, _) => (c.Options is CSharpCompilationOptions { AllowUnsafe: true }, c.AssemblyName));

        IncrementalValueProvider<EmitterOptions> configOptionsData = context.AnalyzerConfigOptionsProvider
            .Select((options, _) =>
            {
                bool generatedIndexOfAnyDebuggerHiddenDisabled = false;
                if (options.GlobalOptions.TryGetValue(GeneratedIndexOfAnyDebuggerHiddenDisabledMetadata, out string? value))
                {
                    generatedIndexOfAnyDebuggerHiddenDisabled = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                }

                return new EmitterOptions(generatedIndexOfAnyDebuggerHiddenDisabled);
            });

        var combined = codeOrDiagnostics
            .Combine(compilationData)
            .Combine(configOptionsData);

        context.RegisterSourceOutput(combined, static (context, compilationData) =>
        {
            ImmutableArray<object?> results = compilationData.Left.Left;
            var (allowUnsafe, assemblyName) = compilationData.Left.Right;
            EmitterOptions emitterOptions   = compilationData.Right;

            bool allFailures                                 = true;
            ImmutableArray<IndexOfAnyMethod>.Builder builder = ImmutableArray.CreateBuilder<IndexOfAnyMethod>();

            // Report any top-level diagnostics.
            foreach (object? result in results)
            {
                if (result is Diagnostic d)
                {
                    context.ReportDiagnostic(d);
                }
                else if (result is IndexOfAnyMethod indexOfAnyMethod)
                {
                    allFailures = false;
                    builder.Add(indexOfAnyMethod);
                }
                else
                {
                    throw new InvalidOperationException("Should not be here");
                }
            }

            if (allFailures)
            {
                return;
            }

            ImmutableArray<IndexOfAnyMethod> indexOfAnyMethods = builder.ToImmutableArray();
            IndexOfAnyEmitter emitter                          = new(emitterOptions);

            emitter.Emit(context, indexOfAnyMethods);
        });
    }
}
