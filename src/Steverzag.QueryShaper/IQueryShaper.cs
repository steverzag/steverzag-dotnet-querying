using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Ordering;

namespace Steverzag.QueryShaper
{
	public interface IQueryShaper
	{
		IQueryable<T> ApplySearch<T>(IQueryable<T> source, string? search);

		IQueryable<T> ApplyFilters<T>(IQueryable<T> source, IReadOnlyList<FilterNode> filters);

		IQueryable<T> ApplyOrder<T>(IQueryable<T> source, IReadOnlyList<OrderParameters> order);

		IQueryable<TOut> Map<TIn, TOut>(IQueryable<TIn> source);

		PageRequest ResolvePage(PageRequest? page);

		IQueryable<T> Apply<T>(IQueryable<T> source, QueryRequest request);

		IQueryable<TOut> Apply<TIn, TOut>(IQueryable<TIn> source, QueryRequest request);
	}
}
