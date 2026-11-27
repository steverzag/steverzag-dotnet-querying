using Steverzag.QueryShaper.Pagination;

namespace Steverzag.QueryShaper
{
	public static class QueryShaperPagingExtensions
	{
		public static PaginatedList<T> ApplyPaged<T>(this IQueryShaper shaper, IQueryable<T> source, QueryRequest request)
		{
			ArgumentNullException.ThrowIfNull(shaper);
			ArgumentNullException.ThrowIfNull(request);
			var page = DefaultQueryShaper.Within("page", () => shaper.ResolvePage(request.Page));
			return shaper.Apply(source, request).Paginate(page);
		}

		public static PaginatedList<TOut> ApplyPaged<TIn, TOut>(this IQueryShaper shaper, IQueryable<TIn> source, QueryRequest request)
		{
			ArgumentNullException.ThrowIfNull(shaper);
			ArgumentNullException.ThrowIfNull(request);
			var page = DefaultQueryShaper.Within("page", () => shaper.ResolvePage(request.Page));
			return shaper.Apply<TIn, TOut>(source, request).Paginate(page);
		}
	}
}
