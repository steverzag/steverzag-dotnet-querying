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
		/// Applies a request as <see cref="IQueryShaper.Apply{T}"/> does, then reads its page asynchronously. A request with
		/// no page gets page 1 at the default size, so it never returns every row.
		/// </summary>
		/// <typeparam name="T">The source type.</typeparam>
		/// <param name="shaper">The shaper.</param>
		/// <param name="source">The query. It must come from EF Core.</param>
		/// <param name="request">The request.</param>
		/// <param name="cancellationToken">Cancels the queries.</param>
		/// <returns>The page.</returns>
		/// <exception cref="QueryRequestException">The request is invalid. A page error's path starts with <c>page</c>, such as <c>page.size</c>.</exception>
		public static Task<PaginatedList<T>> ApplyPagedAsync<T>(this IQueryShaper shaper, IQueryable<T> source, QueryRequest request, CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(shaper);
			ArgumentNullException.ThrowIfNull(request);
			var page = ResolvePage(shaper, request);
			return shaper.Apply(source, request).PaginateAsync(page, cancellationToken);
		}

		/// <summary>
		/// Applies a request as <see cref="IQueryShaper.Apply{TIn, TOut}"/> does, map included, then reads its page
		/// asynchronously. A request with no page gets page 1 at the default size.
		/// </summary>
		/// <typeparam name="TIn">The source type.</typeparam>
		/// <typeparam name="TOut">The destination type.</typeparam>
		/// <param name="shaper">The shaper.</param>
		/// <param name="source">The query. It must come from EF Core.</param>
		/// <param name="request">The request.</param>
		/// <param name="cancellationToken">Cancels the queries.</param>
		/// <returns>The page.</returns>
		/// <exception cref="QueryRequestException">The request is invalid. A page error's path starts with <c>page</c>, such as <c>page.size</c>.</exception>
		/// <example>
		/// <code>
		/// app.MapPost("/products/query", (QueryRequest request, AppDb db, IQueryShaper shaper, CancellationToken ct)
		///     => shaper.ApplyPagedAsync&lt;Product, ProductDto&gt;(db.Products, request, ct));
		/// </code>
		/// </example>
		public static Task<PaginatedList<TOut>> ApplyPagedAsync<TIn, TOut>(this IQueryShaper shaper, IQueryable<TIn> source, QueryRequest request, CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(shaper);
			ArgumentNullException.ThrowIfNull(request);
			var page = ResolvePage(shaper, request);
			return shaper.Apply<TIn, TOut>(source, request).PaginateAsync(page, cancellationToken);
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
