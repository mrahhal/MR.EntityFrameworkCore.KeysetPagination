using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace MR.EntityFrameworkCore.KeysetPagination.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class KeysetContainsNullableColumnAnalyzer : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
		DiagnosticDescriptors.KeysetPagination1000_KeysetContainsNullableColumn);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

		context.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);
	}

	private void AnalyzeOperation(OperationAnalysisContext context)
	{
		switch (context.Operation.Kind)
		{
			case OperationKind.Invocation:
				AnalyzeInvocation(context, (IInvocationOperation)context.Operation);
				break;
		}
	}

	private void AnalyzeInvocation(OperationAnalysisContext context, IInvocationOperation operation)
	{
		if (!IsCandidateMethod(operation, operation.TargetMethod, out var argument))
		{
			return;
		}

		// The argument is the lambda. Traverse down and find the return operation.
		var returnOperation = argument.Descendants().OfType<IReturnOperation>().FirstOrDefault();
		if (returnOperation == null) return;

		// Find out the type of the expression.
		var typeInfo = operation.SemanticModel?.GetTypeInfo(returnOperation.Syntax);
		if (typeInfo == null) return;

		if (typeInfo.Value.Nullability.FlowState == NullableFlowState.MaybeNull)
		{
			context.ReportDiagnostic(Diagnostic.Create(
				DiagnosticDescriptors.KeysetPagination1000_KeysetContainsNullableColumn,
				returnOperation.Syntax.GetLocation()));
		}
	}

	private bool IsCandidateMethod(
		IInvocationOperation operation,
		IMethodSymbol method,
		out IArgumentOperation argument)
	{
		if (operation.Arguments.Length > 0 &&
			method.ContainingNamespace.Name == "KeysetPagination" &&
			method.ContainingType.Name == "KeysetPaginationBuilder" &&
			(method.Name == "Ascending" || method.Name == "Descending"))
		{
			// Should always be valid for the methods above.
			argument = operation.Arguments[0];
			return true;
		}

		argument = null!;
		return false;
	}
}
