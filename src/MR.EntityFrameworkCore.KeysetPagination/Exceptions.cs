namespace MR.EntityFrameworkCore.KeysetPagination;

public class KeysetPaginationIncompatibleObjectException : Exception
{
	public KeysetPaginationIncompatibleObjectException(string message)
		: base(message + " Refer to the following document for more info on loose typing: https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/blob/main/docs/loose-typing.md")
	{
	}
}
