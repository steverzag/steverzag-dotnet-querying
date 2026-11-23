using Microsoft.EntityFrameworkCore;

// Same namespace as the core pagination types so callers only need one using.
namespace Steverzag.QueryShaper.Pagination
{
	public static class EntityFrameworkPaginationExtensions
	{
		/// <summary>
		/// Asynchronously counts and pages <paramref name="source"/> using EF Core's async query operators.
		/// </summary>
		public static async Task<PaginatedList<T>> PaginateAsync<T>(this IQueryable<T> source, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
		{
			var count = await source.CountAsync(cancellationToken);
			var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
			return new PaginatedList<T>(items, count, pageIndex, pageSize);
		}
	}
}
