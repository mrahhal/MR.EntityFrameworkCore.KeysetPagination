using Microsoft.CodeAnalysis;

namespace TemplateRoslynAnalyzer;

public static class DiagnosticDescriptors
{
	public static class Categories
	{
		public const string Trivia = nameof(Trivia);
	}

	// https://github.com/dotnet/roslyn-analyzers/issues/5828
#pragma warning disable IDE0090 // Use 'new(...)'
	public static readonly DiagnosticDescriptor MRKB1000_AvoidSomething = new DiagnosticDescriptor(
		"MRKP1000",
		"Avoid something",
		"Avoid something",
		Categories.Trivia,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true);
#pragma warning restore IDE0090 // Use 'new(...)'
}
