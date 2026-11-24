using System.Collections;

namespace Steverzag.QueryShaper.Pagination
{
	public sealed class PaginatedList<T> : IReadOnlyList<T>
	{

		private readonly T[] _items;
		public int PageIndex { get; }
		public int PageSize { get; }
		public int TotalCount { get; }

		public int TotalPages { get; }

		public PaginatedList(IEnumerable<T> items, int count, int pageIndex, int pageSize)
		{
			ArgumentNullException.ThrowIfNull(items);
			PaginationGuard.ValidatePage(pageIndex, pageSize);
			ArgumentOutOfRangeException.ThrowIfNegative(count);

			_items = [.. items];
			PageIndex = pageIndex;
			PageSize = pageSize;
			TotalCount = count;
			TotalPages = (int)Math.Ceiling(count / (double)pageSize);
		}

		public int Count => _items.Length;

		public T this[int index] => _items[index];

		public bool HasPreviousPage => PageIndex > 1;

		public bool HasNextPage => PageIndex < TotalPages;

		public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public static PaginatedList<T> Create(IQueryable<T> source, int pageIndex, int pageSize)
		{
			ArgumentNullException.ThrowIfNull(source);
			PaginationGuard.ValidatePage(pageIndex, pageSize);

			var count = source.Count();
			var items = PaginationGuard.TryGetSkip(pageIndex, pageSize, out var skip)
				? source.Skip(skip).Take(pageSize).ToList()
				: [];
			return new PaginatedList<T>(items, count, pageIndex, pageSize);
		}
	}
}
