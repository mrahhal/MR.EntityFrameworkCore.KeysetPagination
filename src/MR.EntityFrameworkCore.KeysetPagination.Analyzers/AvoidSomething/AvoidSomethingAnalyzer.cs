using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace TemplateRoslynAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AvoidSomethingAnalyzer : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
		DiagnosticDescriptors.MRKB1000_AvoidSomething);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

		context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
	}

	private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
	{
		context.ReportDiagnostic(Diagnostic.Create(
			DiagnosticDescriptors.MRKB1000_AvoidSomething,
			context.Tree.GetRoot().GetLocation()));
	}
}
