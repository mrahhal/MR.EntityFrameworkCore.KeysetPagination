namespace MR.EntityFrameworkCore.KeysetPagination;

public class KeysetPaginationIncompatibleObjectException : Exception
{
	public KeysetPaginationIncompatibleObjectException(string message)
		: base(message + " Refer to the following document for more info: https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/blob/main/docs/loose-typing.md")
	{
	}
}

public class KeysetPaginationUnexpectedNullException : Exception
{
	public KeysetPaginationUnexpectedNullException(string propertyName)
		: base($"Unexpected null value for '{propertyName}'. You shouldn't have a null in your keyset as it will most definitely result in the wrong results returned. Refer to the following document for more info: https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/blob/main/docs/caveats.md#null")
	{
	}
}
