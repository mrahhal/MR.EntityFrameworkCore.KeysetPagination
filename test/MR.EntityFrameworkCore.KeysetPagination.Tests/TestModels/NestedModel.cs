namespace MR.EntityFrameworkCore.KeysetPagination.TestModels;

public class NestedModel
{
	public int Id { get; set; }

	public NestedInnerModel Inner { get; set; }
}

public class NestedInnerModel
{
	public int Id { get; set; }

	public DateTime Created { get; set; }
}
