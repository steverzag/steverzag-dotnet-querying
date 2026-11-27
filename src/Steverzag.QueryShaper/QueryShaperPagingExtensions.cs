using Steverzag.QueryShaper.Pagination;

namespace Steverzag.QueryShaper
{
	/// <summary>Applies a whole request, page included, and reads the page.</summary>
	public static class QueryShaperPagingExtensions
	{
		/// <summary>
		/// Applies a request as <see cref="IQueryShaper.Apply{T}"/> does, then reads its page. A request with no page gets
		/// page 1 at the default size, so it never returns every row.
		/// </summary>
		/// <typeparam name="T">The source type.</typeparam>
		/// <param name="shaper">The shaper.</param>
		/// <param name="source">The query.</param>
		/// <param name="request">The request.</param>
		/// <returns>The page.</returns>
		/// <exception cref="Exceptions.QueryRequestException">The request is invalid. A page error's path starts with <c>page</c>, such as <c>page.size</c>.</exception>
		public static PaginatedList<T> ApplyPaged<T>(this IQueryShaper shaper, IQueryable<T> source, QueryRequest request)
		{
			ArgumentNullException.ThrowIfNull(shaper);
			ArgumentNullException.ThrowIfNull(request);
			var page = DefaultQueryShaper.Within("page", () => shaper.ResolvePage(request.Page));
			return shaper.Apply(source, request).Paginate(page);
		}

		/// <summary>
		/// Applies a request as <see cref="IQueryShaper.Apply{TIn, TOut}"/> does, map included, then reads its page. A
		/// request with no page gets page 1 at the default size.
		/// </summary>
		/// <typeparam name="TIn">The source type.</typeparam>
		/// <typeparam name="TOut">The destination type.</typeparam>
		/// <param name="shaper">The shaper.</param>
		/// <param name="source">The query.</param>
		/// <param name="request">The request.</param>
		/// <returns>The page.</returns>
		/// <exception cref="Exceptions.QueryRequestException">The request is invalid. A page error's path starts with <c>page</c>, such as <c>page.size</c>.</exception>
		public static PaginatedList<TOut> ApplyPaged<TIn, TOut>(this IQueryShaper shaper, IQueryable<TIn> source, QueryRequest request)
		{
			ArgumentNullException.ThrowIfNull(shaper);
			ArgumentNullException.ThrowIfNull(request);
			var page = DefaultQueryShaper.Within("page", () => shaper.ResolvePage(request.Page));
			return shaper.Apply<TIn, TOut>(source, request).Paginate(page);
		}
	}
}
