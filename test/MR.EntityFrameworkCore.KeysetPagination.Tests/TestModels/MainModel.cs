using Microsoft.EntityFrameworkCore;

namespace MR.EntityFrameworkCore.KeysetPagination.TestModels;

[Index(nameof(String))]
[Index(nameof(Guid))]
[Index(nameof(IsDone))]
[Index(nameof(Created))]
[Index(nameof(CreatedComputed))]
public class MainModel
{
	public int Id { get; set; }

	public string String { get; set; }

	public Guid Guid { get; set; }

	public bool IsDone { get; set; }

	public DateTime Created { get; set; }

	public DateTime? CreatedNullable { get; set; }

	public DateTime CreatedComputed { get; }

	public NestedInnerModel Inner { get; set; }
}

[Index(nameof(Created))]
public class NestedInnerModel
{
	public int Id { get; set; }

	public DateTime Created { get; set; }
}
