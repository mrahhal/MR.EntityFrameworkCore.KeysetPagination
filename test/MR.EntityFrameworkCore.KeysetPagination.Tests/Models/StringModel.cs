using System;

namespace MR.EntityFrameworkCore.KeysetPagination.Tests.Models
{
	public class StringModel
	{
		public StringModel()
		{
			Id = Guid.NewGuid().ToString();
		}

		public string Id { get; set; }

		public DateTime Created { get; set; }
	}
}
