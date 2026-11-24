using Microsoft.EntityFrameworkCore;

// Same namespace as the core pagination types so callers only need one using.
namespace Steverzag.QueryShaper.Pagination
{
	public static class EntityFrameworkPaginationExtensions
	{
		/// <summary>
		/// Asynchronously counts and pages <paramref name="source"/> using EF Core's async query operators.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="pageIndex"/> or <paramref name="pageSize"/> is less than 1.</exception>
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
	}
}
