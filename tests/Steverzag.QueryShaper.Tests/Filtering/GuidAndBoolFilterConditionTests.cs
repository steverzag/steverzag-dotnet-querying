using Steverzag.QueryShaper.Exceptions;
using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Tests.Infrastructure;

namespace Steverzag.QueryShaper.Tests.Filtering
{
	public class GuidAndBoolFilterConditionTests(SqliteDatabaseFixture database) : FilterTestBase(database)
	{
		// ---- Guid. Code: one per row. OptionalCode: 1 → Code1, 2 → null, 3 → Code3, 4 → Code4

		[Fact]
		public void Guid_Equals()
		{
			Assert.Equal([2], FilterIds(nameof(Product.Code), FilterCondition.Equals, SeedData.Code2));
		}

		[Fact]
		public void Guid_NotEquals()
		{
			Assert.Equal([1, 3, 4], FilterIds(nameof(Product.Code), FilterCondition.NotEquals, SeedData.Code2));
		}

		[Fact]
		public void Guid_Equals_AcceptsStringValue()
		{
			Assert.Equal([2], FilterIds(nameof(Product.Code), FilterCondition.Equals, SeedData.Code2.ToString()));
		}

		[Fact]
		public void NullableGuid_Equals()
		{
			Assert.Equal([3], FilterIds(nameof(Product.OptionalCode), FilterCondition.Equals, SeedData.Code3));
		}

		[Fact]
		public void NullableGuid_IsEmpty_MatchesNull()
		{
			Assert.Equal([2], FilterIds(nameof(Product.OptionalCode), FilterCondition.IsEmpty));
		}

		[Fact]
		public void NullableGuid_IsNotEmpty_ExcludesNull()
		{
			Assert.Equal([1, 3, 4], FilterIds(nameof(Product.OptionalCode), FilterCondition.IsNotEmpty));
		}

		[Theory]
		[InlineData(FilterCondition.IsEmpty)]
		[InlineData(FilterCondition.IsNotEmpty)]
		public void NonNullableGuid_EmptinessCondition_Throws(FilterCondition condition)
		{
			Assert.Throws<FilterConditionAndValueNotSupportedException>(() => FilterIds(nameof(Product.Code), condition));
		}

		[Theory]
		[InlineData(FilterCondition.GreaterThan)]
		[InlineData(FilterCondition.Contains)]
		public void Guid_UnsupportedCondition_Throws(FilterCondition condition)
		{
			Assert.Throws<FilterNotSupportedException>(() => FilterIds(nameof(Product.Code), condition, SeedData.Code1));
		}

		// ---- bool. IsActive: true, true, false, true. IsFeatured: true, null, false, true

		[Fact]
		public void Bool_Equals()
		{
			Assert.Equal([1, 2, 4], FilterIds(nameof(Product.IsActive), FilterCondition.Equals, true));
		}

		[Fact]
		public void Bool_NotEquals()
		{
			Assert.Equal([3], FilterIds(nameof(Product.IsActive), FilterCondition.NotEquals, true));
		}

		[Fact]
		public void Bool_NullValue_Throws()
		{
			// Decision 013.2. A missing value used to be read as false.
			Assert.Throws<FilterConditionAndValueNotSupportedException>(() => FilterIds(nameof(Product.IsActive), FilterCondition.Equals));
		}

		[Theory]
		[InlineData(nameof(Product.Code))]
		[InlineData(nameof(Product.OptionalCode))]
		public void Guid_NullValue_Throws(string field)
		{
			// A missing value used to be read as Guid.Empty.
			Assert.Throws<FilterConditionAndValueNotSupportedException>(() => FilterIds(field, FilterCondition.Equals));
		}

		[Fact]
		public void NullableBool_Equals()
		{
			Assert.Equal([1, 4], FilterIds(nameof(Product.IsFeatured), FilterCondition.Equals, true));
		}

		[Fact]
		public void NullableBool_NotEquals_IncludesNullRows()
		{
			Assert.Equal([2, 3], FilterIds(nameof(Product.IsFeatured), FilterCondition.NotEquals, true));
		}

		[Fact]
		public void NullableBool_IsEmpty_MatchesNull()
		{
			Assert.Equal([2], FilterIds(nameof(Product.IsFeatured), FilterCondition.IsEmpty));
		}

		[Fact]
		public void NullableBool_IsNotEmpty_ExcludesNull()
		{
			Assert.Equal([1, 3, 4], FilterIds(nameof(Product.IsFeatured), FilterCondition.IsNotEmpty));
		}

		[Theory]
		[InlineData(FilterCondition.IsEmpty)]
		[InlineData(FilterCondition.IsNotEmpty)]
		public void NonNullableBool_EmptinessCondition_Throws(FilterCondition condition)
		{
			Assert.Throws<FilterConditionAndValueNotSupportedException>(() => FilterIds(nameof(Product.IsActive), condition));
		}

		[Theory]
		[InlineData(FilterCondition.GreaterThan)]
		[InlineData(FilterCondition.StartsWith)]
		public void Bool_UnsupportedCondition_Throws(FilterCondition condition)
		{
			Assert.Throws<FilterNotSupportedException>(() => FilterIds(nameof(Product.IsActive), condition, true));
		}
	}
}
