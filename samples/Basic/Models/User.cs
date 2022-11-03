using Microsoft.EntityFrameworkCore;

#nullable enable

namespace Basic.Models
{
	[Index(nameof(Created), nameof(Id))]
	public class User
	{
		public int Id { get; set; }

		public string Name { get; set; } = default!;

		public DateTime Created { get; set; }

		// Used to show how to deal with nullables in the keyset
		public DateTime? NullableDate { get; set; }
		public DateTime NullableDateComputed { get; }
		// ---

		public UserDetails Details { get; set; } = default!;

		public UserDetails? NullableDetails { get; set; }
	}

	[Index(nameof(Created))]
	public class UserDetails
	{
		public int Id { get; set; }

		public DateTime Created { get; set; }
	}
}
