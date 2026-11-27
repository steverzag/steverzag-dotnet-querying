using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Ordering;

namespace Steverzag.QueryShaper
{
	/// <summary>
	/// Applies client requests to a query. It validates its input, applies the <see cref="QueryShaperOptions"/> and their
	/// guard rails, and reports every error in the request as a <see cref="Exceptions.QueryRequestException"/>.
	/// </summary>
	/// <remarks>
	/// Register it with <c>AddQueryShaper</c> and inject it. For queries built in code from trusted input, the extension
	/// methods (<c>ApplyLogicsFilter</c>, <c>ApplyOrder</c>, …) do the same work without the checks; don't pass client
	/// input to them.
	/// </remarks>
	/// <example>
	/// <code>
	/// app.MapPost("/products/query", (QueryRequest request, AppDb db, IQueryShaper shaper) =>
	/// {
	///     var page = shaper.ApplyPaged&lt;Product, ProductDto&gt;(db.Products.OrderBy(p => p.Id), request);
	///     return new { items = page, page.PageIndex, page.PageSize, page.TotalCount, page.TotalPages };
	/// });
	/// </code>
	/// </example>
	public interface IQueryShaper
	{
		/// <summary>Applies the search configured for <typeparamref name="T"/>. A blank search leaves the query unchanged.</summary>
		/// <typeparam name="T">The source type.</typeparam>
		/// <param name="source">The query.</param>
		/// <param name="search">The text to search for.</param>
		/// <returns>The filtered query.</returns>
		/// <exception cref="Exceptions.QueryRequestException">The search is longer than <see cref="QueryShaperOptions.MaxSearchLength"/>.</exception>
		/// <exception cref="InvalidOperationException">The search isn't blank and no search is configured for <typeparamref name="T"/>, which is a server error.</exception>
		IQueryable<T> ApplySearch<T>(IQueryable<T> source, string? search);

		/// <summary>Applies filters, combined left to right.</summary>
		/// <typeparam name="T">The source type.</typeparam>
		/// <param name="source">The query.</param>
		/// <param name="filters">The filters.</param>
		/// <returns>The filtered query.</returns>
		/// <exception cref="Exceptions.QueryRequestException">A filter is invalid or over a cap. Its path is relative to <paramref name="filters"/>, such as <c>[1].filters[0].field</c>.</exception>
		IQueryable<T> ApplyFilters<T>(IQueryable<T> source, IReadOnlyList<FilterNode> filters);

		/// <summary>
		/// Applies orders, the first as the primary sort, then the default order configured for <typeparamref name="T"/>
		/// as a tie-breaker. With no orders, the default alone, unless <paramref name="source"/> is already ordered.
		/// </summary>
		/// <typeparam name="T">The source type.</typeparam>
		/// <param name="source">The query.</param>
		/// <param name="order">The orders.</param>
		/// <returns>The ordered query, or <paramref name="source"/> if there's nothing to order by.</returns>
		/// <exception cref="Exceptions.QueryRequestException">An order is invalid, on an unsortable type, or over <see cref="QueryShaperOptions.MaxOrders"/>.</exception>
		IQueryable<T> ApplyOrder<T>(IQueryable<T> source, IReadOnlyList<OrderParameters> order);

		/// <summary>Projects the query with the map registered from <typeparamref name="TIn"/> to <typeparamref name="TOut"/>.</summary>
		/// <typeparam name="TIn">The source type.</typeparam>
		/// <typeparam name="TOut">The destination type.</typeparam>
		/// <param name="source">The query.</param>
		/// <returns>The projected query.</returns>
		IQueryable<TOut> Map<TIn, TOut>(IQueryable<TIn> source);

		/// <summary>Fills in the default page size, and checks the page against the options.</summary>
		/// <param name="page">The requested page, or <see langword="null"/> for page 1 at the default size.</param>
		/// <returns>The page, with its size set.</returns>
		/// <exception cref="Exceptions.QueryRequestException">The index is below 1, or the size is below 1 or above <see cref="QueryShaperOptions.MaxPageSize"/>. The path is <c>index</c> or <c>size</c>.</exception>
		PageRequest ResolvePage(PageRequest? page);

		/// <summary>Applies a request's search, then its filters, then its order. The page isn't applied; use <c>ApplyPaged</c> for that.</summary>
		/// <typeparam name="T">The source type.</typeparam>
		/// <param name="source">The query.</param>
		/// <param name="request">The request.</param>
		/// <returns>The query, searched, filtered and ordered.</returns>
		/// <exception cref="Exceptions.QueryRequestException">The request is invalid. The path points at the part, such as <c>filters[1].filters[0].field</c>.</exception>
		IQueryable<T> Apply<T>(IQueryable<T> source, QueryRequest request);

		/// <summary>
		/// Applies a request as <see cref="Apply{T}"/> does, and makes sure the rows have an order to page by. The page
		/// isn't applied; <c>ApplyPaged</c> and <c>ApplyPagedAsync</c> call this, then read it.
		/// </summary>
		/// <remarks>
		/// The order is the request's, then a tie-breaker: the default order configured for <typeparamref name="T"/>, or
		/// <paramref name="fallbackOrder"/> when none is. With no request order, a source that's already ordered keeps its
		/// order; otherwise the tie-breaker alone orders it.
		/// </remarks>
		/// <typeparam name="T">The source type.</typeparam>
		/// <param name="source">The query.</param>
		/// <param name="request">The request.</param>
		/// <param name="fallbackOrder">An order to use when no default is configured, such as the primary key the EF add-on passes. It's trusted: it isn't validated.</param>
		/// <returns>The query, searched, filtered and ordered.</returns>
		/// <exception cref="Exceptions.QueryRequestException">The request is invalid.</exception>
		/// <exception cref="InvalidOperationException">Nothing orders the rows: no request order, an unordered source, no default order and no fallback. That's a server error.</exception>
		IQueryable<T> ApplyForPaging<T>(IQueryable<T> source, QueryRequest request, IReadOnlyList<OrderParameters>? fallbackOrder = null);

		/// <summary>Applies a request as <see cref="Apply{T}"/> does, then the registered map. Selectors name the source type's properties.</summary>
		/// <typeparam name="TIn">The source type.</typeparam>
		/// <typeparam name="TOut">The destination type.</typeparam>
		/// <param name="source">The query.</param>
		/// <param name="request">The request.</param>
		/// <returns>The projected query.</returns>
		/// <exception cref="Exceptions.QueryRequestException">The request is invalid.</exception>
		IQueryable<TOut> Apply<TIn, TOut>(IQueryable<TIn> source, QueryRequest request);
	}
}
