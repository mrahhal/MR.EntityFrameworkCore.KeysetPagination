using Microsoft.EntityFrameworkCore;

namespace Basic.Models
{
	[Index(nameof(Created), nameof(Id))]
	public class User
	{
		public int Id { get; set; }

		public string Name { get; set; }

		public DateTime Created { get; set; }

		public UserDetails Details { get; set; }
	}

	[Index(nameof(Created))]
	public class UserDetails
	{
		public int Id { get; set; }

		public DateTime Created { get; set; }
	}
}
