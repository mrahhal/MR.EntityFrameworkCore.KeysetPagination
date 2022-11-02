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

	public DbSet<IntModel> IntModels { get; set; }

	public DbSet<StringModel> StringModels { get; set; }

	public DbSet<GuidModel> GuidModels { get; set; }

	public DbSet<NestedModel> NestedModels { get; set; }

	public DbSet<ComputedModel> ComputedModels { get; set; }

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
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

		var computedPropertyBuilder = builder.Entity<ComputedModel>()
			.Property(x => x.CreatedComputed);

		// We're coalescing NULLs into a max date.
		// This results in NULLs effectively being sorted last (if ASC), irrelevant of the Db.
		if (DatabaseFixture.UseSqlServer)
		{
			// For Sql Server:
			computedPropertyBuilder
				// Has to be deterministic to be able to create an index for it, that's why we need
				// to use CONVERT.
				.HasComputedColumnSql("COALESCE(Created, CONVERT(datetime2, '9999-12-31', 102))");
		}
		else
		{
			// For sqlite:
			computedPropertyBuilder
				// This is how EF formats dates for sqlite. Be careful, you'll have to put the
				// right format or you might get wrong results.
				.HasComputedColumnSql("COALESCE(Created, '9999-12-31 00:00:00')");
		}

		// Make sure to properly index columns as per your expected queries.
		builder.Entity<ComputedModel>()
			.HasIndex(x => x.CreatedComputed);
	}
}
