using Steverzag.QueryShaper.Exceptions;
using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Tests.Infrastructure;

namespace Steverzag.QueryShaper.Tests.Filtering
{
	public class CollectionFilterConditionTests(SqliteDatabaseFixture database) : FilterTestBase(database)
	{
		// Tags: 1 [tools, steel], 2 [garden], 3 [], 4 [garden, tools]
		// Reviews.Stars: 1 [5, 4], 2 [3], 3 [], 4 [4, 4, 2]

		[Fact]
		public void Contains_MatchesSubstringOfAnyElement()
		{
			Assert.Equal([2, 4], FilterIds(nameof(Product.Tags), FilterCondition.Contains, "gar"));
		}

		[Fact]
		public void DoesNotContain()
		{
			Assert.Equal([1, 3], FilterIds(nameof(Product.Tags), FilterCondition.DoesNotContain, "gar"));
		}

		[Fact]
		public void ContainsAny()
		{
			Assert.Equal([1, 2, 4], FilterIds(nameof(Product.Tags), FilterCondition.ContainsAny, new List<object> { "steel", "garden" }));
		}

		[Fact]
		public void ContainsAny_SingleValue()
		{
			Assert.Equal([1], FilterIds(nameof(Product.Tags), FilterCondition.ContainsAny, new List<object> { "steel" }));
		}

		[Fact]
		public void ContainsAny_ExactMatchOnly()
		{
			// Unlike Contains (substring of any element), ContainsAny compares whole elements.
			Assert.Empty(FilterIds(nameof(Product.Tags), FilterCondition.ContainsAny, new List<object> { "gar" }));
		}

		[Fact]
		public void ContainsAny_EmptyList_Throws()
		{
			Assert.Throws<ArgumentException>(() => FilterIds(nameof(Product.Tags), FilterCondition.ContainsAny, new List<object>()));
		}

		[Fact]
		public void DoesNotContainAny()
		{
			Assert.Equal([3], FilterIds(nameof(Product.Tags), FilterCondition.DoesNotContainAny, new List<object> { "steel", "garden" }));
		}

		[Fact]
		public void ContainsAll()
		{
			Assert.Equal([4], FilterIds(nameof(Product.Tags), FilterCondition.ContainsAll, new List<object> { "garden", "tools" }));
		}

		[Fact]
		public void DoesNotContainAll()
		{
			Assert.Equal([1, 2, 3], FilterIds(nameof(Product.Tags), FilterCondition.DoesNotContainAll, new List<object> { "garden", "tools" }));
		}

		[Fact]
		public void ContainsAll_EmptyList_Throws()
		{
			Assert.Throws<ArgumentException>(() => FilterIds(nameof(Product.Tags), FilterCondition.ContainsAll, new List<object>()));
		}

		[Fact]
		public void IsEmpty_MatchesEmptyCollection()
		{
			Assert.Equal([3], FilterIds(nameof(Product.Tags), FilterCondition.IsEmpty));
		}

		[Fact]
		public void IsNotEmpty_ExcludesEmptyCollection()
		{
			Assert.Equal([1, 2, 4], FilterIds(nameof(Product.Tags), FilterCondition.IsNotEmpty));
		}

		// Projected collection over a navigation: Reviews.Stars → x.Reviews.Select(r => r.Stars)

		[Fact]
		public void Projected_ContainsAll()
		{
			Assert.Equal([1, 4], FilterIds("Reviews.Stars", FilterCondition.ContainsAll, new List<object> { 4 }));
		}

		[Fact]
		public void Projected_ContainsAny()
		{
			Assert.Equal([2, 4], FilterIds("Reviews.Stars", FilterCondition.ContainsAny, new List<object> { 2, 3 }));
		}

		[Fact]
		public void Projected_IsEmpty()
		{
			Assert.Equal([3], FilterIds("Reviews.Stars", FilterCondition.IsEmpty));
		}

		// Enum collection. StatusHistory: 1 [Draft, Active], 2 [Draft], 3 [Draft, Active, Discontinued], 4 [Active]

		[Fact]
		public void EnumElements_ContainsAll()
		{
			Assert.Equal([1, 3], FilterIds(nameof(Product.StatusHistory), FilterCondition.ContainsAll,
				new List<object> { (int)ProductStatus.Draft, (int)ProductStatus.Active }));
		}

		[Fact]
		public void EnumElements_ContainsAny()
		{
			Assert.Equal([3], FilterIds(nameof(Product.StatusHistory), FilterCondition.ContainsAny,
				new List<object> { (int)ProductStatus.Discontinued }));
		}

		[Fact]
		public void EnumElements_ContainsAny_ByName()
		{
			Assert.Equal([1, 3, 4], FilterIds(nameof(Product.StatusHistory), FilterCondition.ContainsAny,
				new List<object> { "Active", "Discontinued" }));
		}

		[Theory]
		[InlineData(FilterCondition.ContainsAny)]
		[InlineData(FilterCondition.DoesNotContainAny)]
		[InlineData(FilterCondition.ContainsAll)]
		[InlineData(FilterCondition.DoesNotContainAll)]
		public void EnumElements_UndefinedValue_Throws(FilterCondition condition)
		{
			// Decision 008.2.
			Assert.Throws<FilterConditionAndValueNotSupportedException>(() => FilterIds(nameof(Product.StatusHistory), condition,
				new List<object> { (int)ProductStatus.Active, 99 }));
		}

		// Nullable elements. Measurements: 1 [1, null], 2 [2], 3 [], 4 [1, 2]

		[Fact]
		public void NullableElements_ContainsAll()
		{
			Assert.Equal([4], FilterIds(nameof(Product.Measurements), FilterCondition.ContainsAll, new List<object> { 1, 2 }));
		}

		[Fact]
		public void NullableElements_ContainsAny_Null()
		{
			Assert.Equal([1], FilterIds(nameof(Product.Measurements), FilterCondition.ContainsAny, new List<object?> { null }!));
		}

		[Fact]
		public void NullableElements_ContainsAny()
		{
			Assert.Equal([2, 4], FilterIds(nameof(Product.Measurements), FilterCondition.ContainsAny, new List<object> { 2 }));
		}

		// Collections of entities have no scalar to compare a value with.

		[Theory]
		[InlineData(FilterCondition.ContainsAll)]
		[InlineData(FilterCondition.DoesNotContainAll)]
		[InlineData(FilterCondition.ContainsAny)]
		[InlineData(FilterCondition.DoesNotContainAny)]
		public void EntityCollection_ValueListCondition_IsNotSupported(FilterCondition condition)
		{
			Assert.Throws<FilterNotSupportedException>(() => FilterIds(nameof(Product.Reviews), condition, new List<object> { 1 }));
		}

		[Fact]
		public void EntityCollection_IsEmpty_StillWorks()
		{
			Assert.Equal([3], FilterIds(nameof(Product.Reviews), FilterCondition.IsEmpty));
		}

		[Fact]
		public void Contains_OnNonStringElements_Throws()
		{
			// Contains / DoesNotContain on collections only exist for string elements.
			Assert.Throws<FilterNotSupportedException>(() => FilterIds("Reviews.Stars", FilterCondition.Contains, 4));
		}

		[Fact]
		public void UnsupportedPropertyType_Throws()
		{
			Assert.Throws<FilterNotSupportedException>(() => FilterIds(nameof(Product.Warranty), FilterCondition.Equals, 1));
		}
	}
}
