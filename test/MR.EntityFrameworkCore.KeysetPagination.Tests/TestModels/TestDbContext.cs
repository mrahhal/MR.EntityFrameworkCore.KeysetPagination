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
	}

	public IEnumerable<string> LogMessages => _logMessages;

	public DbSet<IntModel> IntModels { get; set; }

	public DbSet<StringModel> StringModels { get; set; }

	public DbSet<GuidModel> GuidModels { get; set; }

	public DbSet<NestedModel> NestedModels { get; set; }
}
