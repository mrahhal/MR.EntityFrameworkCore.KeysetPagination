using Microsoft.EntityFrameworkCore;

public class BenchmarkDbContext : DbContext
{
	public BenchmarkDbContext(DbContextOptions options) : base(options)
	{
	}

	public DbSet<User> Users { get; set; } = default!;
}

[Index(nameof(Created), nameof(Id))]
public class User
{
	public int Id { get; set; }

	public string Name { get; set; } = default!;

	public DateTime Created { get; set; }
}
