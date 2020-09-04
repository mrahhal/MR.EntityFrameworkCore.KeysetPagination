using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Basic.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MR.EntityFrameworkCore.KeysetPagination;

namespace Basic.Pages
{
	public class IndexModel : PageModel
	{
		private readonly AppDbContext _dbContext;
		private readonly ILogger _logger;

		public IndexModel(
			AppDbContext dbContext,
			ILogger<IndexModel> logger)
		{
			_dbContext = dbContext;
			_logger = logger;
		}

		public List<User> Users { get; set; }

		public bool HasPrevious { get; set; }

		public bool HasNext { get; set; }

		public async Task OnGet(int? after, int? before)
		{
			var query = _dbContext.Users.AsQueryable();
			var count = await query.CountAsync();
			KeysetPaginationContext<User> keysetContext;
			if (after != null)
			{
				var reference = await _dbContext.Users.FindAsync(after.Value);
				keysetContext = query.KeysetPaginate(b => b.Ascending(x => x.Id), reference, KeysetPaginationReferenceDirection.After);
				Users = await keysetContext.Query
				  .Take(20)
				  .ToListAsync();
			}
			else if (before != null)
			{
				var reference = await _dbContext.Users.FindAsync(before.Value);
				keysetContext = query.KeysetPaginate(b => b.Ascending(x => x.Id), reference, KeysetPaginationReferenceDirection.Before);
				Users = await keysetContext.Query
				  .Take(20)
				  .ToListAsync();
			}
			else
			{
				keysetContext = query.KeysetPaginate(b => b.Ascending(x => x.Id));
				Users = await keysetContext.Query
				  .Take(20)
				  .ToListAsync();
			}

			HasPrevious = await keysetContext.HasPreviousAsync(Users);
			HasNext = await keysetContext.HasNextAsync(Users);
		}
	}
}
