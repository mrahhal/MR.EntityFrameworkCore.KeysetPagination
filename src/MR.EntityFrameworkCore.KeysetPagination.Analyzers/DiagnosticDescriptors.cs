using Microsoft.CodeAnalysis;

namespace MR.EntityFrameworkCore.KeysetPagination.Analyzers;

public static class DiagnosticDescriptors
{
	public static class Categories
	{
		public const string Usage = nameof(Usage);
	}

	public static readonly DiagnosticDescriptor KeysetPagination1000_KeysetContainsNullableColumn = new(
		"KeysetPagination1000",
		"Keyset column may be null",
		"Keyset column may be null",
		Categories.Usage,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "Nullable columns are not supported in the keyset.",
		helpLinkUri: "https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/blob/main/docs/diagnostics.md#KeysetPagination1000");
}
