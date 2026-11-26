using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Ordering;
using Steverzag.QueryShaper.Pagination;
using Steverzag.QueryShaper.Tests.Infrastructure;

namespace Steverzag.QueryShaper.Tests.Pagination
{
	public class PaginationTests(SqliteDatabaseFixture database) : DatabaseTestBase(database)
	{
		// 4 products, ids 1..4. Every query is ordered by Id so that pages are deterministic.

		private PaginatedList<int> Page(int pageIndex, int pageSize)
		{
			using var context = Database.CreateContext();
			return context.Products.OrderBy(p => p.Id).Select(p => p.Id).Paginate(pageIndex, pageSize);
		}

		private async Task<PaginatedList<int>> PageAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
		{
			await using var context = Database.CreateContext();
			return await context.Products.OrderBy(p => p.Id).Select(p => p.Id).PaginateAsync(pageIndex, pageSize, cancellationToken);
		}

		private static void AssertPage(PaginatedList<int> page, int[] items, int pageIndex, int totalPages, bool hasPrevious, bool hasNext, int? totalCount = null, int? pageSize = null)
		{
			Assert.Equal(items, page);
			Assert.Equal(pageIndex, page.PageIndex);
			Assert.Equal(totalPages, page.TotalPages);
			Assert.Equal(hasPrevious, page.HasPreviousPage);
			Assert.Equal(hasNext, page.HasNextPage);
			if (totalCount is not null) Assert.Equal(totalCount, page.TotalCount);
			if (pageSize is not null) Assert.Equal(pageSize, page.PageSize);
		}

		// ---- regular pages

		[Fact]
		public void FirstPage()
		{
			AssertPage(Page(1, 2), items: [1, 2], pageIndex: 1, totalPages: 2, hasPrevious: false, hasNext: true);
		}

		[Fact]
		public void LastFullPage()
		{
			AssertPage(Page(2, 2), items: [3, 4], pageIndex: 2, totalPages: 2, hasPrevious: true, hasNext: false);
		}

		[Fact]
		public void MiddlePage()
		{
			AssertPage(Page(2, 1), items: [2], pageIndex: 2, totalPages: 4, hasPrevious: true, hasNext: true);
		}

		[Fact]
		public void PartialLastPage()
		{
			AssertPage(Page(2, 3), items: [4], pageIndex: 2, totalPages: 2, hasPrevious: true, hasNext: false);
		}

		[Fact]
		public void PageSizeLargerThanTheSource_IsASinglePage()
		{
			AssertPage(Page(1, 10), items: [1, 2, 3, 4], pageIndex: 1, totalPages: 1, hasPrevious: false, hasNext: false);
		}

		// ---- edges

		[Fact]
		public void PageAfterTheLast_IsEmpty()
		{
			AssertPage(Page(3, 2), items: [], pageIndex: 3, totalPages: 2, hasPrevious: true, hasNext: false);
		}

		[Fact]
		public void EmptySource_HasNoPages()
		{
			using var context = Database.CreateContext();
			var page = context.Products.Where(p => p.Id < 0).Select(p => p.Id).Paginate(1, 2);

			AssertPage(page, items: [], pageIndex: 1, totalPages: 0, hasPrevious: false, hasNext: false, totalCount: 0, pageSize: 2);
		}

		[Fact]
		public void FarPageWhoseOffsetOverflowsInt_IsEmpty()
		{
			// (int.MaxValue - 1) * 2 doesn't fit in an int. It used to wrap to a negative Skip, which SQLite reads as 0,
			// so the "page" came back with the first rows.
			AssertPage(Page(int.MaxValue, 2), items: [], pageIndex: int.MaxValue, totalPages: 2, hasPrevious: true, hasNext: false, totalCount: 4, pageSize: 2);
		}

		// ---- TotalCount and PageSize

		[Theory]
		[InlineData(1, 2, 2)]
		[InlineData(2, 3, 1)]
		[InlineData(3, 2, 0)]
		public void TotalCountAndPageSize_DescribeTheRequest_NotThisPage(int pageIndex, int pageSize, int itemsOnPage)
		{
			var page = Page(pageIndex, pageSize);

			Assert.Equal(itemsOnPage, page.Count);
			Assert.Equal(4, page.TotalCount);
			Assert.Equal(pageSize, page.PageSize);
		}

		// ---- argument validation (decision 009.7)

		[Theory]
		[InlineData(0)]
		[InlineData(-1)]
		[InlineData(int.MinValue)]
		public void PageIndexBelowOne_Throws(int pageIndex)
		{
			// Page 0 used to return page 1's items with PageIndex 0 (SQLite reads a negative OFFSET as 0).
			Assert.Throws<ArgumentOutOfRangeException>("pageIndex", () => Page(pageIndex, 2));
		}

		[Theory]
		[InlineData(0)]
		[InlineData(-1)]
		[InlineData(int.MinValue)]
		public void PageSizeBelowOne_Throws(int pageSize)
		{
			// Size 0 used to report int.MaxValue pages; size -1 became LIMIT -1 and returned every row.
			Assert.Throws<ArgumentOutOfRangeException>("pageSize", () => Page(1, pageSize));
		}

		[Fact]
		public void InvalidArguments_ThrowBeforeTheQueryRuns()
		{
			// The source is never enumerated: a query that would fail if it ran proves nothing was sent.
			var neverRun = new[] { 1 }.AsQueryable().Select(_ => ThrowIfEvaluated());

			Assert.Throws<ArgumentOutOfRangeException>("pageIndex", () => neverRun.Paginate(0, 2));
			Assert.Throws<ArgumentOutOfRangeException>("pageSize", () => neverRun.Paginate(1, 0));
		}

		private static int ThrowIfEvaluated() => throw new InvalidOperationException("The query ran.");

		[Fact]
		public void NullSource_Throws()
		{
			Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null!).Paginate(1, 2));
		}

		// ---- the PaginatedList constructor (for callers that page by themselves)

		[Fact]
		public void Constructor_SetsEveryProperty()
		{
			var page = new PaginatedList<int>([5, 6], count: 7, pageIndex: 3, pageSize: 2);

			AssertPage(page, items: [5, 6], pageIndex: 3, totalPages: 4, hasPrevious: true, hasNext: true, totalCount: 7, pageSize: 2);
		}

		// ---- read-only

		[Fact]
		public void IsReadOnly_NotAMutableCollection()
		{
			// Guards against the type going back to List<T> (or picking up a mutable interface) later.
			var interfaces = typeof(PaginatedList<int>).GetInterfaces();

			Assert.Contains(typeof(IReadOnlyList<int>), interfaces);
			Assert.DoesNotContain(typeof(ICollection<int>), interfaces);
			Assert.DoesNotContain(typeof(System.Collections.IList), interfaces);
			Assert.Equal(typeof(object), typeof(PaginatedList<int>).BaseType);
		}

		[Fact]
		public void IndexerAndCount_ReadThisPage()
		{
			var page = Page(2, 3);

			Assert.Equal(4, Assert.Single(page));
			Assert.Equal(4, page[0]);
			Assert.Throws<IndexOutOfRangeException>(() => page[1]);
		}

		[Fact]
		public void Constructor_CopiesTheItems()
		{
			List<int> items = [5, 6];
			var page = new PaginatedList<int>(items, count: 7, pageIndex: 3, pageSize: 2);

			items.Add(7);
			items[0] = 99;

			Assert.Equal([5, 6], page);
		}

		[Fact]
		public void Constructor_ValidatesItsArguments()
		{
			Assert.Throws<ArgumentOutOfRangeException>("pageIndex", () => new PaginatedList<int>([], 0, 0, 2));
			Assert.Throws<ArgumentOutOfRangeException>("pageSize", () => new PaginatedList<int>([], 0, 1, 0));
			Assert.Throws<ArgumentOutOfRangeException>("count", () => new PaginatedList<int>([], -1, 1, 2));
			Assert.Throws<ArgumentNullException>("items", () => new PaginatedList<int>(null!, 0, 1, 2));
		}

		// ---- composition with filtering and ordering

		[Fact]
		public void FilterThenOrderThenPaginate()
		{
			// active: Hammer (1), Shovel (2), Rake (4). By name: Hammer, Rake, Shovel.
			using var context = Database.CreateContext();
			var page = context.Products
				.ApplyLogicsFilter([new ConditionNode { FieldSelector = nameof(Product.IsActive), FilterCondition = FilterCondition.Equals, Value = true }])
				.ApplyOrder([new OrderParameters(nameof(Product.Name), OrderDirection.Ascending)])
				.Select(p => p.Id)
				.Paginate(1, 2);

			AssertPage(page, items: [1, 4], pageIndex: 1, totalPages: 2, hasPrevious: false, hasNext: true);
		}

		[Fact]
		public void TotalPages_CountsTheFilteredRows_NotTheWholeTable()
		{
			using var context = Database.CreateContext();
			var page = context.Products
				.ApplyLogicsFilter([new ConditionNode { FieldSelector = nameof(Product.IsActive), FilterCondition = FilterCondition.Equals, Value = false }])
				.OrderBy(p => p.Id)
				.Select(p => p.Id)
				.Paginate(1, 2);

			AssertPage(page, items: [3], pageIndex: 1, totalPages: 1, hasPrevious: false, hasNext: false);
		}

		// ---- PaginateAsync (Steverzag.QueryShaper.EntityFrameworkCore)

		[Theory]
		[InlineData(1, 2)]
		[InlineData(2, 2)]
		[InlineData(2, 3)]
		[InlineData(3, 2)]
		[InlineData(1, 10)]
		public async Task Async_MatchesSync(int pageIndex, int pageSize)
		{
			var expected = Page(pageIndex, pageSize);
			var actual = await PageAsync(pageIndex, pageSize);

			AssertPage(actual, [.. expected], expected.PageIndex, expected.TotalPages, expected.HasPreviousPage, expected.HasNextPage, expected.TotalCount, expected.PageSize);
		}

		[Fact]
		public async Task Async_FarPageWhoseOffsetOverflowsInt_IsEmpty()
		{
			AssertPage(await PageAsync(int.MaxValue, 2), items: [], pageIndex: int.MaxValue, totalPages: 2, hasPrevious: true, hasNext: false, totalCount: 4, pageSize: 2);
		}

		[Theory]
		[InlineData(0, 2, "pageIndex")]
		[InlineData(-1, 2, "pageIndex")]
		[InlineData(1, 0, "pageSize")]
		[InlineData(1, -1, "pageSize")]
		public async Task Async_InvalidArguments_Throw(int pageIndex, int pageSize, string paramName)
		{
			await Assert.ThrowsAsync<ArgumentOutOfRangeException>(paramName, () => PageAsync(pageIndex, pageSize));
		}

		[Fact]
		public async Task Async_NullSource_Throws()
		{
			await Assert.ThrowsAsync<ArgumentNullException>("source", () => ((IQueryable<int>)null!).PaginateAsync(1, 2));
		}

		[Fact]
		public async Task Async_HonoursCancellation()
		{
			using var cancellation = new CancellationTokenSource();
			cancellation.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => PageAsync(1, 2, cancellation.Token));
		}
	}
}
