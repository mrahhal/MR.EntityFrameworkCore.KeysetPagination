using Basic.Models;
using MR.EntityFrameworkCore.KeysetPagination;

namespace Basic;

#pragma warning disable IDE0051
#nullable enable

// Testing that the analyzer properly detects the following cases.
// Uncommenting the following two pragma suppressions should reveal errors/warnings on HEREs.

#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable KeysetPagination1000 // Keyset contains a nullable property

public class AnalyzerTest
{
	private readonly AppDbContext _dbContext = null!;

	private void TestingTheAnalyzer()
	{
		var analyzerTestKeysetBuilderAction = (KeysetPaginationBuilder<User> b) =>
		{
			//                HERE
			b.Descending(x => x.NullableDate).Ascending(x => x.Id);
		};

		_dbContext.Users.KeysetPaginate(
			//                     HERE
			b => b.Descending(x => x.NullableDate).Ascending(x => x.Id));

		_dbContext.Users.KeysetPaginateQuery(
			//                    HERE
			b => b.Ascending(x => x.NullableDate));

		_dbContext.Users.KeysetPaginateQuery(
			//                    HERE (CS8602 warning)
			b => b.Ascending(x => x.NullableDetails.Id));

		// This should be fine.
		_dbContext.Users.KeysetPaginateQuery(
			b => b.Ascending(x => x.NullableDate ?? DateTime.MinValue));
	}
}
