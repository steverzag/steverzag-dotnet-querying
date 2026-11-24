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

		private static void AssertPage(PaginatedList<int> page, int[] items, int pageIndex, int totalPages, bool hasPrevious, bool hasNext)
		{
			Assert.Equal(items, page);
			Assert.Equal(pageIndex, page.PageIndex);
			Assert.Equal(totalPages, page.TotalPages);
			Assert.Equal(hasPrevious, page.HasPreviousPage);
			Assert.Equal(hasNext, page.HasNextPage);
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

			AssertPage(page, items: [], pageIndex: 1, totalPages: 0, hasPrevious: false, hasNext: false);
		}

		// ---- arguments that are not validated today. These pin the current results; see ROADMAP Phase 2.

		[Fact]
		public void PageZero_IsNotRejected_AndReturnsTheFirstPageItems()
		{
			// Skip(-2): SQLite treats a negative OFFSET as 0, so the items are page 1's, but PageIndex stays 0.
			AssertPage(Page(0, 2), items: [1, 2], pageIndex: 0, totalPages: 2, hasPrevious: false, hasNext: true);
		}

		[Fact]
		public void PageSizeZero_IsNotRejected_AndReportsInfinitePages()
		{
			// Take(0) returns nothing, and TotalPages = (int)Ceiling(4 / 0.0) = (int)Infinity, which .NET saturates to int.MaxValue.
			AssertPage(Page(1, 0), items: [], pageIndex: 1, totalPages: int.MaxValue, hasPrevious: false, hasNext: true);
		}

		[Fact]
		public void NegativePageSize_IsNotRejected_AndReturnsEveryRow()
		{
			// Take(-1) becomes LIMIT -1, which SQLite reads as "no limit". TotalPages is Ceiling(4 / -1.0) = -4.
			AssertPage(Page(1, -1), items: [1, 2, 3, 4], pageIndex: 1, totalPages: -4, hasPrevious: false, hasNext: false);
		}

		// ---- composition with filtering and ordering

		[Fact]
		public void FilterThenOrderThenPaginate()
		{
			// active: Hammer (1), Shovel (2), Rake (4). By name: Hammer, Rake, Shovel.
			using var context = Database.CreateContext();
			var page = context.Products
				.ApplyLogicsFilter([new ConditionNode { FieldSelector = nameof(Product.IsActive), FilterCondition = FilterCondition.Equals, SearchValue = true }])
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
				.ApplyLogicsFilter([new ConditionNode { FieldSelector = nameof(Product.IsActive), FilterCondition = FilterCondition.Equals, SearchValue = false }])
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

			AssertPage(actual, [.. expected], expected.PageIndex, expected.TotalPages, expected.HasPreviousPage, expected.HasNextPage);
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
