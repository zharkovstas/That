using Microsoft.CodeAnalysis;

namespace That.Analyzers;

internal static class Descriptors
{
    internal static readonly DiagnosticDescriptor That0001 = new(
        id: "That0001",
        title: "Specify assertion message",
        messageFormat: "Specify a message to print when the assertion fails. Replace this call in '{0}' with a call to '{1}'.",
        category: "Assertions",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);
}