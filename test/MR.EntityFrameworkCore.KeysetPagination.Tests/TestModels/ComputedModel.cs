namespace MR.EntityFrameworkCore.KeysetPagination.TestModels;

public class ComputedModel
{
	public int Id { get; set; }

	public DateTime? Created { get; set; }

	public DateTime CreatedComputed { get; }
}
