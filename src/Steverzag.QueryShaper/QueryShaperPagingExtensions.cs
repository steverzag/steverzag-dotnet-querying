using Steverzag.QueryShaper.Pagination;

namespace Steverzag.QueryShaper
{
	/// <summary>Applies a whole request, page included, and reads the page.</summary>
	public static class QueryShaperPagingExtensions
	{
		/// <summary>
		/// Applies a request as <see cref="IQueryShaper.ApplyForPaging{T}"/> does, then reads its page. A request with no
		/// page gets page 1 at the default size, so it never returns every row.
		/// </summary>
		/// <remarks>
		/// The rows need an order: the request's, the source's, or the default configured with <c>ConfigureDefaultOrder</c>.
		/// On EF Core, the add-on's <c>ApplyPagedAsync</c> also falls back to the primary key.
		/// </remarks>
		/// <typeparam name="T">The source type.</typeparam>
		/// <param name="shaper">The shaper.</param>
		/// <param name="source">The query.</param>
		/// <param name="request">The request.</param>
		/// <returns>The page.</returns>
		/// <exception cref="Exceptions.QueryRequestException">The request is invalid. A page error's path starts with <c>page</c>, such as <c>page.size</c>.</exception>
		/// <exception cref="InvalidOperationException">Nothing orders the rows, which is a server error.</exception>
		public static PaginatedList<T> ApplyPaged<T>(this IQueryShaper shaper, IQueryable<T> source, QueryRequest request)
		{
			ArgumentNullException.ThrowIfNull(shaper);
			ArgumentNullException.ThrowIfNull(request);
			var page = DefaultQueryShaper.Within("page", () => shaper.ResolvePage(request.Page));
			return shaper.ApplyForPaging(source, request).Paginate(page);
		}

		/// <summary>
		/// Applies a request as <see cref="IQueryShaper.ApplyForPaging{T}"/> does, then the registered map, then reads its
		/// page. A request with no page gets page 1 at the default size.
		/// </summary>
		/// <remarks>The rows need an order, as for <see cref="ApplyPaged{T}"/>.</remarks>
		/// <typeparam name="TIn">The source type.</typeparam>
		/// <typeparam name="TOut">The destination type.</typeparam>
		/// <param name="shaper">The shaper.</param>
		/// <param name="source">The query.</param>
		/// <param name="request">The request.</param>
		/// <returns>The page.</returns>
		/// <exception cref="Exceptions.QueryRequestException">The request is invalid. A page error's path starts with <c>page</c>, such as <c>page.size</c>.</exception>
		/// <exception cref="InvalidOperationException">Nothing orders the rows, which is a server error.</exception>
		public static PaginatedList<TOut> ApplyPaged<TIn, TOut>(this IQueryShaper shaper, IQueryable<TIn> source, QueryRequest request)
		{
			ArgumentNullException.ThrowIfNull(shaper);
			ArgumentNullException.ThrowIfNull(request);
			var page = DefaultQueryShaper.Within("page", () => shaper.ResolvePage(request.Page));
			return shaper.Map<TIn, TOut>(shaper.ApplyForPaging(source, request)).Paginate(page);
		}
	}
}
