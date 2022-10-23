using Microsoft.EntityFrameworkCore;

namespace MR.EntityFrameworkCore.KeysetPagination.TestModels;

public class TestDbContext : DbContext
{
	public TestDbContext(
		DbContextOptions<TestDbContext> options)
		: base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder builder)
	{
		base.OnModelCreating(builder);
	}

	public DbSet<IntModel> IntModels { get; set; }

	public DbSet<StringModel> StringModels { get; set; }

	public DbSet<GuidModel> GuidModels { get; set; }

	public DbSet<NullableModel> NullableModels { get; set; }

	public DbSet<NestedModel> NestedModels { get; set; }
}
