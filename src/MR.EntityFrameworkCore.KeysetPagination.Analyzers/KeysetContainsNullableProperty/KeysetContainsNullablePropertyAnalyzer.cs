using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

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

		// The argument is the lambda (IConversionOperation). Traverse down to find the first property ref.
		var prop = argument.Descendants()
			.OfType<IPropertyReferenceOperation>()
			.FirstOrDefault();
		if (prop != null && prop.Property.NullableAnnotation == NullableAnnotation.Annotated)
		{
			context.ReportDiagnostic(Diagnostic.Create(
				DiagnosticDescriptors.KeysetPagination1000_KeysetContainsNullableProperty,
				prop.Syntax.GetLocation(),
				prop.Property.Name));
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
