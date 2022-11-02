using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace MR.EntityFrameworkCore.KeysetPagination.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class KeysetContainsNullablePropertyAnalyzer : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
		DiagnosticDescriptors.KeysetPagination1000_KeysetContainsNullableProperty);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

		context.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);
	}

	private void AnalyzeOperation(OperationAnalysisContext context)
	{
		//context.ReportDiagnostic(Diagnostic.Create(
		//	DiagnosticDescriptors.KeysetPagination1000_KeysetContainsNullableProperty,
		//	context.Operation.Syntax.GetLocation(),
		//	"Foo"));

		switch (context.Operation.Kind)
		{
			case OperationKind.Invocation:
				AnalyzeInvocation(context, (IInvocationOperation)context.Operation);
				break;
		}
	}

	private void AnalyzeInvocation(OperationAnalysisContext context, IInvocationOperation operation)
	{
		if (!IsCandidateMethod(operation, operation.TargetMethod))
		{
			return;
		}

		context.ReportDiagnostic(Diagnostic.Create(
			DiagnosticDescriptors.KeysetPagination1000_KeysetContainsNullableProperty,
			operation.Arguments[0].Syntax.GetLocation(),
			"Foo"));
	}

	private bool IsCandidateMethod(IInvocationOperation operation, IMethodSymbol method)
	{
		return operation.Arguments.Length > 0
			&& method.ContainingType.Name == "KeysetPaginationBuilder"
			//&& method.ContainingNamespace ==
			&& (method.Name == "Ascending" || method.Name == "Descending");
	}
}
