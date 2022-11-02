using Basic.Models;
using Microsoft.EntityFrameworkCore;

namespace Basic
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		public void ConfigureServices(IServiceCollection services)
		{
			services.AddRazorPages();

			services.AddDbContext<AppDbContext>(options => options.UseSqlite("Data Source=app.db"));
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			{
				var random = new Random();
				var dataCountToAdd = 1_000_003;
				Console.WriteLine($"Adding {dataCountToAdd} rows.");

				using var scope = app.ApplicationServices.CreateScope();
				var _dbContext = scope.ServiceProvider.GetService<AppDbContext>();
				_dbContext.Database.EnsureDeleted();
				_dbContext.Database.EnsureCreated();
				var users = new List<User>(capacity: dataCountToAdd);

				var now = DateTime.Now.AddYears(-1);
				for (var i = 1; i <= dataCountToAdd; i++)
				{
					var created = now.AddMinutes(i).AddSeconds(random.NextDouble() * 50);
					users.Add(new User
					{
						Id = i,
						Name = i.ToString(),
						Created = created,
						NullableDate = i % 2 == 0 ? created : null,
						Details = new UserDetails
						{
							Created = created,
						},
					});
				}
				_dbContext.AddRange(users);
				_dbContext.SaveChanges();

				Console.WriteLine($"Added {dataCountToAdd} rows.");
			}

			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("/Error");
				app.UseHsts();
			}

			app.UseHttpsRedirection();
			app.UseStaticFiles();

			app.UseRouting();

			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapRazorPages();
			});
		}
	}
}
