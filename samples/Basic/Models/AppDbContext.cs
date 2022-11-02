using Microsoft.EntityFrameworkCore;

namespace Basic.Models
{
	public class AppDbContext : DbContext
	{
		public AppDbContext(
			DbContextOptions<AppDbContext> options)
			: base(options)
		{
		}

		public DbSet<User> Users { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<User>()
				.Property(x => x.NullableDateComputed)
				// We're coalescing NULLs into a max date. This results in NULLs effectively sorted last (if ASC), irrelevant of the db provider.
				// You're writing sql here, make sure the results are what you expect for your particular database.
				.HasComputedColumnSql("COALESCE(NullableDate, '9999-12-31T00:00:00.0000000')");

			// Make sure to properly index columns as per your expected queries.
			modelBuilder.Entity<User>()
				.HasIndex(x => new { x.NullableDateComputed, x.Id });
		}
	}
}
