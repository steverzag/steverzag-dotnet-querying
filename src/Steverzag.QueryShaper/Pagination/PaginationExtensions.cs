namespace Steverzag.QueryShaper.Pagination
{
	/// <summary>Pages a query.</summary>
	/// <remarks>
	/// For trusted input built in code: these methods have no options and no guard rails. Pass client requests
	/// through <see cref="IQueryShaper"/>, which validates them first.
	/// </remarks>
	public static class PaginationExtensions
	{
		/// <summary>
		/// Counts the query and reads one page of it, in two queries. Order the query first, or the pages aren't
		/// deterministic. For EF Core, <c>PaginateAsync</c> in <c>Steverzag.QueryShaper.EntityFrameworkCore</c> does the
		/// same asynchronously.
		/// </summary>
		/// <typeparam name="T">The row type.</typeparam>
		/// <param name="source">The query.</param>
		/// <param name="pageIndex">The page, 1-based. A page after the last one comes back empty, with the real counts.</param>
		/// <param name="pageSize">The page size.</param>
		/// <returns>The page.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="pageIndex"/> or <paramref name="pageSize"/> is below 1.</exception>
		public static PaginatedList<T> Paginate<T>(this IQueryable<T> source, int pageIndex, int pageSize)
			=> PaginatedList<T>.Create(source, pageIndex, pageSize);

		/// <summary>Counts the query and reads the requested page of it, in two queries.</summary>
		/// <typeparam name="T">The row type.</typeparam>
		/// <param name="source">The query.</param>
		/// <param name="page">The page. Its size must be set; <see cref="IQueryShaper.ResolvePage"/> fills in the default.</param>
		/// <returns>The page.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="page"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">The page has no size.</exception>
		/// <exception cref="ArgumentOutOfRangeException">The page's index or size is below 1.</exception>
		public static PaginatedList<T> Paginate<T>(this IQueryable<T> source, PageRequest page)
			=> PaginatedList<T>.Create(source, page?.Index ?? throw new ArgumentNullException(nameof(page)), PaginationGuard.RequireSize(page));
	}
}
