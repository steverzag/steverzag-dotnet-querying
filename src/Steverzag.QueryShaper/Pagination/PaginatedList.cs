using System.Collections;

namespace Steverzag.QueryShaper.Pagination
{
	/// <summary>One page of a query's rows, with the counts a client needs to page through the rest.</summary>
	/// <remarks>
	/// It can't be changed after it's built, so its items always agree with <see cref="TotalCount"/> and
	/// <see cref="TotalPages"/>. Because it's a collection, System.Text.Json writes it as a plain array of the items; wrap
	/// it in your own response type to send the counts too.
	/// </remarks>
	/// <typeparam name="T">The row type.</typeparam>
	public sealed class PaginatedList<T> : IReadOnlyList<T>
	{
		private readonly T[] _items;

		/// <summary>The requested page, 1-based.</summary>
		public int PageIndex { get; }

		/// <summary>The requested page size. The last page may hold fewer items.</summary>
		public int PageSize { get; }

		/// <summary>The number of rows in the whole source, after any filters.</summary>
		public int TotalCount { get; }

		/// <summary>The number of pages, or 0 for an empty source.</summary>
		public int TotalPages { get; }

		/// <summary>Creates a page from rows already read.</summary>
		/// <param name="items">The page's rows.</param>
		/// <param name="count">The number of rows in the whole source.</param>
		/// <param name="pageIndex">The page, 1-based.</param>
		/// <param name="pageSize">The page size.</param>
		/// <exception cref="ArgumentNullException"><paramref name="items"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="pageIndex"/> or <paramref name="pageSize"/> is below 1, or <paramref name="count"/> is negative.</exception>
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

		/// <summary>The number of rows on this page.</summary>
		public int Count => _items.Length;

		/// <summary>Gets a row of this page.</summary>
		/// <param name="index">The 0-based position on this page.</param>
		public T this[int index] => _items[index];

		/// <summary>Whether there is a page before this one.</summary>
		public bool HasPreviousPage => PageIndex > 1;

		/// <summary>Whether there is a page after this one.</summary>
		public bool HasNextPage => PageIndex < TotalPages;

		/// <summary>Returns an enumerator over this page's rows.</summary>
		/// <returns>The enumerator.</returns>
		public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		/// <summary>
		/// Counts the source and reads one page of it, in two queries. Order the source first, or the pages aren't
		/// deterministic. A page after the last one comes back empty, with the real counts.
		/// </summary>
		/// <param name="source">The query.</param>
		/// <param name="pageIndex">The page, 1-based.</param>
		/// <param name="pageSize">The page size.</param>
		/// <returns>The page.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="pageIndex"/> or <paramref name="pageSize"/> is below 1.</exception>
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
