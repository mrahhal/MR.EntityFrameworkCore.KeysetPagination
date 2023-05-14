using Microsoft.EntityFrameworkCore;

namespace MR.EntityFrameworkCore.KeysetPagination.TestModels;

public class TestDbContext : DbContext
{
	private readonly List<string> _logMessages = new();

	public TestDbContext(
		DbContextOptions<TestDbContext> options)
		: base(options)
	{
	}

	public IEnumerable<string> LogMessages => _logMessages;

	public DbSet<MainModel> MainModels { get; set; }

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

		optionsBuilder.LogTo(message =>
		{
			lock (_logMessages)
			{
				_logMessages.Add(message);
			}
		});
	}

	protected override void OnModelCreating(ModelBuilder builder)
	{
		base.OnModelCreating(builder);

		var computedPropertyBuilder = builder.Entity<MainModel>()
			.Property(x => x.CreatedComputed);

		// We're coalescing NULLs into a max date.
		// This results in NULLs effectively being sorted last (if ASC), irrelevant of the Db.
		if (Database.IsSqlServer())
		{
			// For Sql Server:
			computedPropertyBuilder
				// Has to be deterministic to be able to create an index for it, that's why we need
				// to use CONVERT.
				.HasComputedColumnSql("COALESCE(CreatedNullable, CONVERT(datetime2, '1900-01-01', 102))");
		}
		else
		{
			// For sqlite:
			computedPropertyBuilder
				// This is how EF formats dates for sqlite. Be careful, you'll have to put the
				// right format or you might get wrong results.
				.HasComputedColumnSql("COALESCE(CreatedNullable, '1900-01-01 00:00:00')");
		}
	}
}
