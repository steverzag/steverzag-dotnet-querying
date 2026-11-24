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

		// Decision 008.2: undefined enum values are rejected, except combinations of a [Flags] enum.
		[Theory]
		[InlineData(FilterCondition.Equals, 99)]
		[InlineData(FilterCondition.NotEquals, 99)]
		[InlineData(FilterCondition.Equals, -1)]
		[InlineData(FilterCondition.Equals, 3)] // Active | Discontinued: not a combination, Status isn't [Flags]
		public void UndefinedValue_Throws(FilterCondition condition, int value)
		{
			Assert.Throws<FilterConditionAndValueNotSupportedException>(() => FilterIds(nameof(Product.Status), condition, value));
		}

		[Fact]
		public void UndefinedValue_OnNullable_Throws()
		{
			Assert.Throws<FilterConditionAndValueNotSupportedException>(() => FilterIds(nameof(Product.OptionalStatus), FilterCondition.Equals, 99));
		}

		[Fact]
		public void LongValueThatFits_Works()
		{
			Assert.Equal([1, 4], FilterIds(nameof(Product.Status), FilterCondition.Equals, (long)ProductStatus.Active));
		}

		[Fact]
		public void ValueBeyondTheUnderlyingType_Throws()
		{
			// 2^32 + 1: used to wrap to 1 (Active) and match rows 1 and 4.
			Assert.Throws<FilterConditionAndValueNotSupportedException>(() => FilterIds(nameof(Product.Status), FilterCondition.Equals, 4_294_967_297L));
		}

		[Fact]
		public void UndefinedLongValue_Throws()
		{
			Assert.Throws<FilterConditionAndValueNotSupportedException>(() => FilterIds(nameof(Product.Status), FilterCondition.Equals, 99L));
		}

		// Features ([Flags]): 1 Waterproof|Cordless (5), 2 None (0), 3 Foldable (2), 4 Waterproof|Foldable|Cordless (7)

		[Fact]
		public void Flags_Equals_SingleDeclaredValue()
		{
			Assert.Equal([3], FilterIds(nameof(Product.Features), FilterCondition.Equals, (int)ProductFeatures.Foldable));
		}

		[Fact]
		public void Flags_Equals_CombinationOfDeclaredValues()
		{
			Assert.Equal([1], FilterIds(nameof(Product.Features), FilterCondition.Equals,
				(int)(ProductFeatures.Waterproof | ProductFeatures.Cordless)));
		}

		[Fact]
		public void Flags_Equals_AllDeclaredBits()
		{
			Assert.Equal([4], FilterIds(nameof(Product.Features), FilterCondition.Equals, 7));
		}

		[Fact]
		public void Flags_Equals_Zero()
		{
			Assert.Equal([2], FilterIds(nameof(Product.Features), FilterCondition.Equals, (int)ProductFeatures.None));
		}

		[Theory]
		[InlineData(8)]  // an undeclared bit
		[InlineData(9)]  // a declared bit plus an undeclared one
		[InlineData(-1)] // every bit set
		public void Flags_UndeclaredBits_Throw(int value)
		{
			Assert.Throws<FilterConditionAndValueNotSupportedException>(() => FilterIds(nameof(Product.Features), FilterCondition.Equals, value));
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
