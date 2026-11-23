using Steverzag.QueryShaper.Exceptions;
using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Tests.Infrastructure;

namespace Steverzag.QueryShaper.Tests.Filtering
{
	public class StringFilterConditionTests(SqliteDatabaseFixture database) : FilterTestBase(database)
	{
		// Names: 1 Hammer, 2 Shovel, 3 Wrench, 4 Rake

		[Theory]
		[InlineData(FilterCondition.Equals, "Hammer", new[] { 1 })]
		[InlineData(FilterCondition.NotEquals, "Hammer", new[] { 2, 3, 4 })]
		[InlineData(FilterCondition.Contains, "a", new[] { 1, 4 })]
		[InlineData(FilterCondition.DoesNotContain, "a", new[] { 2, 3 })]
		[InlineData(FilterCondition.StartsWith, "Sh", new[] { 2 })]
		[InlineData(FilterCondition.DoesNotStartWith, "Sh", new[] { 1, 3, 4 })]
		[InlineData(FilterCondition.EndsWith, "er", new[] { 1 })]
		[InlineData(FilterCondition.DoesNotEndWith, "er", new[] { 2, 3, 4 })]
		public void ValueConditions_OnName(FilterCondition condition, string value, int[] expected)
		{
			Assert.Equal(expected, FilterIds(nameof(Product.Name), condition, value));
		}

		[Fact]
		public void Contains_IsCaseSensitive_OnSqlite()
		{
			// Provider behaviour, not a QueryShaper decision: SQLite translates string.Contains to instr(),
			// which is case-sensitive. SQL Server with a CI collation would match here.
			Assert.Empty(FilterIds(nameof(Product.Name), FilterCondition.Contains, "hammer"));
		}

		[Fact]
		public void IsEmpty_MatchesNullAndEmptyString()
		{
			Assert.Equal([2, 3], FilterIds(nameof(Product.Description), FilterCondition.IsEmpty));
		}

		[Fact]
		public void IsNotEmpty_ExcludesNullAndEmptyString()
		{
			Assert.Equal([1, 4], FilterIds(nameof(Product.Description), FilterCondition.IsNotEmpty));
		}

		[Fact]
		public void NotEquals_OnNullableColumn_IncludesNullRows()
		{
			// C# semantics (null != "Steel head") rather than SQL three-valued logic.
			Assert.Equal([2, 3, 4], FilterIds(nameof(Product.Description), FilterCondition.NotEquals, "Steel head"));
		}

		[Fact]
		public void DoesNotContain_OnNullableColumn_IncludesNullRows()
		{
			Assert.Equal([2, 3], FilterIds(nameof(Product.Description), FilterCondition.DoesNotContain, "e"));
		}

		[Fact]
		public void NavigationProperty_IsFiltered()
		{
			Assert.Equal([2, 4], FilterIds("Category.Name", FilterCondition.Equals, "Garden"));
		}

		[Theory]
		[InlineData(FilterCondition.Equals)]
		[InlineData(FilterCondition.Contains)]
		[InlineData(FilterCondition.StartsWith)]
		public void NullValue_WithValueCondition_Throws(FilterCondition condition)
		{
			Assert.Throws<FilterConditionAndValueNotSupportedException>(() => FilterIds(nameof(Product.Name), condition));
		}

		[Theory]
		[InlineData(FilterCondition.GreaterThan)]
		[InlineData(FilterCondition.LessThanOrEqual)]
		[InlineData(FilterCondition.ContainsAll)]
		public void UnsupportedCondition_Throws(FilterCondition condition)
		{
			Assert.Throws<FilterNotSupportedException>(() => FilterIds(nameof(Product.Name), condition, "x"));
		}
	}
}
