namespace Steverzag.QueryShaper.Pagination
{
	public static class PaginationExtensions
	{
		/// <summary>
		/// Synchronously counts and pages <paramref name="source"/>. For async execution against EF Core use
		/// <c>PaginateAsync</c> from the <c>Steverzag.QueryShaper.EntityFrameworkCore</c> package.
		/// </summary>
		public static PaginatedList<T> Paginate<T>(this IQueryable<T> source, int pageIndex, int pageSize)
			=> PaginatedList<T>.Create(source, pageIndex, pageSize);
	}
}
