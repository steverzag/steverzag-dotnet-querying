using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Tests.Infrastructure;

namespace Steverzag.QueryShaper.Tests.Filtering
{
	public class InFilterConditionTests(SqliteDatabaseFixture database) : FilterTestBase(database)
	{
		[Fact]
		public void In_Int()
		{
			Assert.Equal([2, 4], FilterIds(nameof(Product.Quantity), FilterCondition.In, new List<object> { 0, 200 }));
		}

		[Fact]
		public void NotIn_Int()
		{
			Assert.Equal([1, 3], FilterIds(nameof(Product.Quantity), FilterCondition.NotIn, new List<object> { 0, 200 }));
		}

		[Fact]
		public void In_String()
		{
			Assert.Equal([1, 4], FilterIds(nameof(Product.Name), FilterCondition.In, new List<object> { "Hammer", "Rake" }));
		}

		[Fact]
		public void In_Guid()
		{
			Assert.Equal([1, 4], FilterIds(nameof(Product.Code), FilterCondition.In, new List<object> { SeedData.Code1, SeedData.Code4 }));
		}

		[Fact]
		public void In_Enum_ByUnderlyingValue()
		{
			Assert.Equal([2, 3], FilterIds(nameof(Product.Status), FilterCondition.In,
				new List<object> { (int)ProductStatus.Draft, (int)ProductStatus.Discontinued }));
		}

		[Fact]
		public void In_DateOnly()
		{
			Assert.Equal([2], FilterIds(nameof(Product.ReleaseDate), FilterCondition.In, new List<object> { SeedData.MidDate.ReleaseDate }));
		}

		// NullableQuantity: 1 → 100, 2 → null, 3 → 0, 4 → 200

		[Fact]
		public void In_NullableInt()
		{
			Assert.Equal([1, 3], FilterIds(nameof(Product.NullableQuantity), FilterCondition.In, new List<object> { 100, 0 }));
		}

		[Fact]
		public void NotIn_NullableInt_IncludesNullRows()
		{
			// C# semantics: null is not in the list.
			Assert.Equal([2, 4], FilterIds(nameof(Product.NullableQuantity), FilterCondition.NotIn, new List<object> { 100, 0 }));
		}

		[Fact]
		public void In_NullableEnum()
		{
			// OptionalStatus: 1 Active, 2 null, 3 Discontinued, 4 Draft
			Assert.Equal([1, 4], FilterIds(nameof(Product.OptionalStatus), FilterCondition.In,
				new List<object> { (int)ProductStatus.Active, (int)ProductStatus.Draft }));
		}

		[Fact]
		public void In_NullableGuid()
		{
			// OptionalCode: 1 Code1, 2 null, 3 Code3, 4 Code4
			Assert.Equal([3], FilterIds(nameof(Product.OptionalCode), FilterCondition.In, new List<object> { SeedData.Code3, SeedData.Code2 }));
		}

		[Fact]
		public void In_NavigationProperty()
		{
			Assert.Equal([1, 3], FilterIds("Category.Name", FilterCondition.In, new List<object> { "Tools" }));
		}

		[Fact]
		public void EmptyList_Throws()
		{
			Assert.Throws<ArgumentException>(() => FilterIds(nameof(Product.Quantity), FilterCondition.In, new List<object>()));
		}

		[Fact]
		public void ValuesOfAnotherNumericType_Throw()
		{
			// Each value must be boxed as the exact property type. A JSON binder that yields long for
			// integers would hit this.
			Assert.Throws<InvalidCastException>(() => FilterIds(nameof(Product.Quantity), FilterCondition.In, new List<object> { 0L, 200L }));
		}

		[Fact]
		public void ScalarValue_Throws()
		{
			Assert.Throws<InvalidCastException>(() => FilterIds(nameof(Product.Quantity), FilterCondition.In, 0));
		}
	}
}
