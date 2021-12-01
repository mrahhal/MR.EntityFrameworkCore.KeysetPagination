namespace MR.EntityFrameworkCore.KeysetPagination;

[Serializable]
public class KeysetPaginationIncompatibleObjectException : Exception
{
	public KeysetPaginationIncompatibleObjectException() { }
	public KeysetPaginationIncompatibleObjectException(string message) : base(message) { }
}
