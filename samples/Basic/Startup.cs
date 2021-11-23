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
				using var scope = app.ApplicationServices.CreateScope();
				var _dbContext = scope.ServiceProvider.GetService<AppDbContext>();
				_dbContext.Database.EnsureDeleted();
				_dbContext.Database.EnsureCreated();

				var now = DateTime.Now.AddYears(-1);
				for (var i = 1; i < 1003; i++)
				{
					var created = now.AddMinutes(i);
					_dbContext.Add(new User
					{
						Id = i,
						Name = i.ToString(),
						Created = created,
					});
				}
				_dbContext.SaveChanges();
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
