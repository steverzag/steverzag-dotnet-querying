using Microsoft.EntityFrameworkCore;
using Steverzag.QueryShaper.Exceptions;

// Same namespace as the core pagination types so callers only need one using.
namespace Steverzag.QueryShaper.Pagination
{
	/// <summary>Pages a query, or applies a whole request, asynchronously with EF Core.</summary>
	public static class EntityFrameworkPaginationExtensions
	{
		/// <summary>
		/// Counts the query and reads one page of it, in two async queries. Order the query first, or the pages aren't
		/// deterministic.
		/// </summary>
		/// <typeparam name="T">The row type.</typeparam>
		/// <param name="source">The query. It must come from EF Core.</param>
		/// <param name="pageIndex">The page, 1-based. A page after the last one comes back empty, with the real counts.</param>
		/// <param name="pageSize">The page size.</param>
		/// <param name="cancellationToken">Cancels the queries.</param>
		/// <returns>The page.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="pageIndex"/> or <paramref name="pageSize"/> is below 1.</exception>
		public static async Task<PaginatedList<T>> PaginateAsync<T>(this IQueryable<T> source, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(source);
			PaginationGuard.ValidatePage(pageIndex, pageSize);

			var count = await source.CountAsync(cancellationToken);
			var items = PaginationGuard.TryGetSkip(pageIndex, pageSize, out var skip)
				? await source.Skip(skip).Take(pageSize).ToListAsync(cancellationToken)
				: [];
			return new PaginatedList<T>(items, count, pageIndex, pageSize);
		}

		/// <summary>Counts the query and reads the requested page of it, in two async queries.</summary>
		/// <typeparam name="T">The row type.</typeparam>
		/// <param name="source">The query. It must come from EF Core.</param>
		/// <param name="page">The page. Its size must be set; <see cref="IQueryShaper.ResolvePage"/> fills in the default.</param>
		/// <param name="cancellationToken">Cancels the queries.</param>
		/// <returns>The page.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="page"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">The page has no size.</exception>
		/// <exception cref="ArgumentOutOfRangeException">The page's index or size is below 1.</exception>
		public static Task<PaginatedList<T>> PaginateAsync<T>(this IQueryable<T> source, PageRequest page, CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(page);
			return source.PaginateAsync(page.Index, PaginationGuard.RequireSize(page), cancellationToken);
		}

		/// <summary>
		/// Applies a request as <see cref="IQueryShaper.ApplyForPaging{T}"/> does, with the entity's primary key as the
		/// fallback order, then reads its page asynchronously. A request with no page gets page 1 at the default size, so it
		/// never returns every row.
		/// </summary>
		/// <remarks>
		/// The primary key breaks ties after the request's order, or orders the page alone, when no default order is
		/// configured for <typeparamref name="T"/>. It's found only when <paramref name="source"/> starts from an entity of
		/// type <typeparamref name="T"/> whose key properties are all CLR properties.
		/// </remarks>
		/// <typeparam name="T">The source type.</typeparam>
		/// <param name="shaper">The shaper.</param>
		/// <param name="source">The query. It must come from EF Core.</param>
		/// <param name="request">The request.</param>
		/// <param name="cancellationToken">Cancels the queries.</param>
		/// <returns>The page.</returns>
		/// <exception cref="QueryRequestException">The request is invalid. A page error's path starts with <c>page</c>, such as <c>page.size</c>.</exception>
		/// <exception cref="InvalidOperationException">Nothing orders the rows, not even a primary key, which is a server error.</exception>
		public static Task<PaginatedList<T>> ApplyPagedAsync<T>(this IQueryShaper shaper, IQueryable<T> source, QueryRequest request, CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(shaper);
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(request);
			var page = ResolvePage(shaper, request);
			return shaper.ApplyForPaging(source, request, PrimaryKeyOrder.For(source)).PaginateAsync(page, cancellationToken);
		}

		/// <summary>
		/// Applies a request as <see cref="ApplyPagedAsync{T}"/> does, primary key fallback included, then the registered
		/// map, and reads its page asynchronously. A request with no page gets page 1 at the default size.
		/// </summary>
		/// <typeparam name="TIn">The source type.</typeparam>
		/// <typeparam name="TOut">The destination type.</typeparam>
		/// <param name="shaper">The shaper.</param>
		/// <param name="source">The query. It must come from EF Core.</param>
		/// <param name="request">The request.</param>
		/// <param name="cancellationToken">Cancels the queries.</param>
		/// <returns>The page.</returns>
		/// <exception cref="QueryRequestException">The request is invalid. A page error's path starts with <c>page</c>, such as <c>page.size</c>.</exception>
		/// <exception cref="InvalidOperationException">Nothing orders the rows, not even a primary key, which is a server error.</exception>
		/// <example>
		/// <code>
		/// app.MapPost("/products/query", async (QueryRequest request, AppDb db, IQueryShaper shaper, CancellationToken ct) =>
		/// {
		///     var page = await shaper.ApplyPagedAsync&lt;Product, ProductDto&gt;(db.Products, request, ct);
		///     return new { items = page, page.PageIndex, page.PageSize, page.TotalCount, page.TotalPages };
		/// });
		/// </code>
		/// </example>
		public static Task<PaginatedList<TOut>> ApplyPagedAsync<TIn, TOut>(this IQueryShaper shaper, IQueryable<TIn> source, QueryRequest request, CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(shaper);
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(request);
			var page = ResolvePage(shaper, request);
			return shaper.Map<TIn, TOut>(shaper.ApplyForPaging(source, request, PrimaryKeyOrder.For(source))).PaginateAsync(page, cancellationToken);
		}

		private static PageRequest ResolvePage(IQueryShaper shaper, QueryRequest request)
		{
			try
			{
				return shaper.ResolvePage(request.Page);
			}
			catch (QueryRequestException ex)
			{
				throw ex.Within("page");
			}
		}
	}
}
