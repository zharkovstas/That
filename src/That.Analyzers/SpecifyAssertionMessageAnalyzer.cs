using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace That.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SpecifyAssertionMessageAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
        = ImmutableArray.Create(Descriptors.That0001);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(context =>
        {
            var assertType = context.Compilation.GetTypeByMetadataName("That.Assert");
            if (assertType is null)
            {
                return;
            }

            context.RegisterOperationAction(
                context =>
                {
                    if (context.Operation is IInvocationOperation
                        {
                            Arguments.Length: 1,
                            TargetMethod: { MethodKind: MethodKind.Ordinary } method
                        } invocation
                        && SymbolEqualityComparer.Default.Equals(method.ContainingType, assertType)
                        && Equals(method.Name, "That"))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                Descriptors.That0001,
                                invocation.Syntax.GetLocation(),
                                context.ContainingSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                                "Assert.That(bool, string)"
                            )
                        );
                    }
                },
            OperationKind.Invocation);
        });
    }
}