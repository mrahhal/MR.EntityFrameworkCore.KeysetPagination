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

		builder.Entity<ComputedModel>()
			.Property(x => x.CreatedComputed)
			// We're coalescing NULLs into a max date. This results in NULLs effectively sorted last (if ASC), irrelevant of the db provider.
			.HasComputedColumnSql("COALESCE(Created, '9999-12-31T00:00:00.0000000')");

		// Make sure to properly index columns as per your expected queries.
		builder.Entity<ComputedModel>()
			.HasIndex(x => x.CreatedComputed);
	}

	public IEnumerable<string> LogMessages => _logMessages;

	public DbSet<IntModel> IntModels { get; set; }

	public DbSet<StringModel> StringModels { get; set; }

	public DbSet<GuidModel> GuidModels { get; set; }

	public DbSet<NestedModel> NestedModels { get; set; }

	public DbSet<ComputedModel> ComputedModels { get; set; }
}
