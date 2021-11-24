using Microsoft.EntityFrameworkCore;

namespace Basic.Models
{
	[Index(nameof(Created))]
	public class User
	{
		public int Id { get; set; }

		public string Name { get; set; }

		public DateTime Created { get; set; }
	}
}
