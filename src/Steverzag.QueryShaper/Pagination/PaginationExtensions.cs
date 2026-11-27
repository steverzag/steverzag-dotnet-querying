namespace Steverzag.QueryShaper.Pagination
{
	/// <remarks>
	/// For trusted input built in code: these methods have no options and no guard rails. Pass client requests
	/// through <see cref="IQueryShaper"/>, which validates them first.
	/// </remarks>
	public static class PaginationExtensions
	{
		/// <summary>
		/// Synchronously counts and pages <paramref name="source"/>. For async execution against EF Core use
		/// <c>PaginateAsync</c> from the <c>Steverzag.QueryShaper.EntityFrameworkCore</c> package.
		/// </summary>
		public static PaginatedList<T> Paginate<T>(this IQueryable<T> source, int pageIndex, int pageSize)
			=> PaginatedList<T>.Create(source, pageIndex, pageSize);

		public static PaginatedList<T> Paginate<T>(this IQueryable<T> source, PageRequest page)
			=> PaginatedList<T>.Create(source, page?.Index ?? throw new ArgumentNullException(nameof(page)), PaginationGuard.RequireSize(page));
	}
}
