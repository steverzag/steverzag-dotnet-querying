using Steverzag.QueryShaper.Exceptions;
using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Tests.Infrastructure;

namespace Steverzag.QueryShaper.Tests.Filtering
{
	public class EnumFilterConditionTests(SqliteDatabaseFixture database) : FilterTestBase(database)
	{
		// Status: 1 Active, 2 Draft, 3 Discontinued, 4 Active
		// OptionalStatus: 1 Active, 2 null, 3 Discontinued, 4 Draft
		// Enum values are passed as their underlying integer.

		[Fact]
		public void Equals_ByUnderlyingValue()
		{
			Assert.Equal([1, 4], FilterIds(nameof(Product.Status), FilterCondition.Equals, (int)ProductStatus.Active));
		}

		[Fact]
		public void NotEquals_ByUnderlyingValue()
		{
			Assert.Equal([2, 3], FilterIds(nameof(Product.Status), FilterCondition.NotEquals, (int)ProductStatus.Active));
		}

		[Fact]
		public void Equals_UndefinedValue_IsAcceptedAndMatchesNothing()
		{
			// Enum.TryParse accepts any numeric string, so an undefined value is not rejected.
			Assert.Empty(FilterIds(nameof(Product.Status), FilterCondition.Equals, 99));
		}

		[Fact]
		public void NullValue_OnNonNullable_Throws()
		{
			// Today this is a bare System.Exception.
			Assert.Throws<Exception>(() => FilterIds(nameof(Product.Status), FilterCondition.Equals));
		}

		[Fact]
		public void StringValue_Throws()
		{
			// Only integer values are accepted; the name "Active" is rejected (as a bare System.Exception).
			Assert.Throws<Exception>(() => FilterIds(nameof(Product.Status), FilterCondition.Equals, "Active"));
		}

		[Theory]
		[InlineData(FilterCondition.GreaterThan)]
		[InlineData(FilterCondition.Contains)]
		public void UnsupportedCondition_Throws(FilterCondition condition)
		{
			Assert.Throws<FilterNotSupportedException>(() => FilterIds(nameof(Product.Status), condition, 1));
		}

		[Fact]
		public void Nullable_Equals()
		{
			Assert.Equal([4], FilterIds(nameof(Product.OptionalStatus), FilterCondition.Equals, (int)ProductStatus.Draft));
		}

		[Fact]
		public void Nullable_NotEquals_IncludesNullRows()
		{
			Assert.Equal([2, 3, 4], FilterIds(nameof(Product.OptionalStatus), FilterCondition.NotEquals, (int)ProductStatus.Active));
		}

		[Fact]
		public void Nullable_IsEmpty_MatchesNull()
		{
			Assert.Equal([2], FilterIds(nameof(Product.OptionalStatus), FilterCondition.IsEmpty));
		}

		[Fact]
		public void Nullable_IsNotEmpty_ExcludesNull()
		{
			Assert.Equal([1, 3, 4], FilterIds(nameof(Product.OptionalStatus), FilterCondition.IsNotEmpty));
		}
	}
}
