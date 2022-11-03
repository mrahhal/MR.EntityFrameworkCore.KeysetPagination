using Microsoft.CodeAnalysis;

namespace MR.EntityFrameworkCore.KeysetPagination.Analyzers;

public static class DiagnosticDescriptors
{
	public static class Categories
	{
		public const string Usage = nameof(Usage);
	}

	// https://github.com/dotnet/roslyn-analyzers/issues/5828
#pragma warning disable IDE0090 // Use 'new(...)'
	public static readonly DiagnosticDescriptor KeysetPagination1000_KeysetContainsNullableProperty = new DiagnosticDescriptor(
		"KeysetPagination1000",
		"Keyset contains a nullable property",
		"Unsupported nullable property '{0}' in the keyset",
		Categories.Usage,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "Nullable properties are not supported in the keyset.",
		helpLinkUri: "https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/blob/main/docs/diagnostics.md#KeysetPagination1000");
#pragma warning restore IDE0090 // Use 'new(...)'
}
