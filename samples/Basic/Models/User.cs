using Microsoft.EntityFrameworkCore;

namespace Basic.Models
{
	[Index(nameof(Created), nameof(Id))]
	public class User
	{
		public int Id { get; set; }

		public string Name { get; set; }

		public DateTime Created { get; set; }
	}
}
