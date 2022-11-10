using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MR.EntityFrameworkCore.KeysetPagination.TestModels
{

	public class NestedJsonModel
	{
		public int Id { get; set; }

		public NestedInnerJsonModel Inner { get; set; }
	}

	public class NestedInnerJsonModel
	{
		public int Id { get; set; }

		public DateTime Created { get; set; }

		public JsonDocument Data { get; set; }
	}
}
