using Steverzag.PropertySelectors;
using Steverzag.QueryShaper.Aggregating;
using Steverzag.QueryShaper.Configuration.Mapping;
using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Ordering;
using Steverzag.QueryShaper.Pagination;
using Steverzag.QueryShaper.Tests.Infrastructure;

namespace Steverzag.QueryShaper.Tests.Mapping
{
	public class ProductSummary
	{
		// filled by name convention (same name on Product)
		public int Id { get; set; }
		public string Name { get; set; } = null!;
		public string? Description { get; set; }

		// filled by ForMember
		public string CategoryName { get; set; } = null!;
		public int ReviewCount { get; set; }
		public double? AverageStars { get; set; }

		// same name, different type: converted (int → long)
		public long Quantity { get; set; }

		// no source and no mapping: left at its default
		public string? Unmapped { get; set; }
	}

	public class ProductLabel
	{
		public int Id { get; set; }
		public string Name { get; set; } = null!;
		public LabelDetails Details { get; set; } = new();
	}

	public class LabelDetails
	{
		public string? Category { get; set; }
	}

	public class ProductQuantity
	{
		public int Id { get; set; }
		public int Quantity { get; set; }
	}

	public class MapperTests(SqliteDatabaseFixture database) : DatabaseTestBase(database)
	{
		// 1 Hammer | "Steel head"  | Tools  | Qty 100 | NullableQty 100  | stars 5, 4
		// 2 Shovel | null          | Garden | Qty 0   | NullableQty null | stars 3
		// 3 Wrench | ""            | Tools  | Qty 50  | NullableQty 0    | no reviews
		// 4 Rake   | "Garden rake" | Garden | Qty 200 | NullableQty 200  | stars 4, 4, 2

		private static Mapper SummaryMapper() => new Mapper().Register(
			new MappingConfiguration<Product, ProductSummary>()
				.ForMember(d => d.CategoryName, "Category.Name")
				.ForMember(d => d.ReviewCount, "Reviews|" + SelectorDefaults.CountAggregateFunction)
				.ForMember(d => d.AverageStars, "Reviews.Stars|" + SelectorDefaults.AvgAggregateFunction));

		private List<TOut> Map<TOut>(IMapper mapper)
		{
			using var context = Database.CreateContext();
			return context.Products.OrderBy(p => p.Id).ApplySelect<Product, TOut>(mapper).ToList();
		}

		// ---- what gets filled

		[Fact]
		public void SameNameProperties_AreCopied()
		{
			var rows = Map<ProductSummary>(SummaryMapper());

			Assert.Equal([1, 2, 3, 4], rows.Select(r => r.Id));
			Assert.Equal(["Hammer", "Shovel", "Wrench", "Rake"], rows.Select(r => r.Name));
			Assert.Equal(["Steel head", null, "", "Garden rake"], rows.Select(r => r.Description));
		}

		[Fact]
		public void ForMember_NavigationPath()
		{
			Assert.Equal(["Tools", "Garden", "Tools", "Garden"], Map<ProductSummary>(SummaryMapper()).Select(r => r.CategoryName));
		}

		[Fact]
		public void ForMember_Aggregates()
		{
			var rows = Map<ProductSummary>(SummaryMapper());

			Assert.Equal([2, 1, 0, 3], rows.Select(r => r.ReviewCount));
			// AVG over no reviews is NULL in SQL, so the nullable destination receives null.
			Assert.Equal([4.5, 3.0, null, 10.0 / 3], rows.Select(r => r.AverageStars));
		}

		[Fact]
		public void SameNameWithAnotherType_IsConverted()
		{
			Assert.Equal([100L, 0L, 50L, 200L], Map<ProductSummary>(SummaryMapper()).Select(r => r.Quantity));
		}

		[Fact]
		public void PropertyWithNoSourceAndNoMapping_KeepsItsDefault()
		{
			Assert.All(Map<ProductSummary>(SummaryMapper()), r => Assert.Null(r.Unmapped));
		}

		[Fact]
		public void ForMember_WinsOverTheNameConvention()
		{
			var mapper = new Mapper().Register(
				new MappingConfiguration<Product, ProductLabel>().ForMember(d => d.Name, "Category.Name"));

			Assert.Equal(["Tools", "Garden", "Tools", "Garden"], Map<ProductLabel>(mapper).Select(r => r.Name));
		}

		[Fact]
		public void ForMember_OnANestedDestination_IsRejected()
		{
			// Only top-level destination properties are bound. A nested one used to be accepted and then
			// silently never applied.
			Assert.Throws<ArgumentException>(() =>
				new MappingConfiguration<Product, ProductLabel>().ForMember(d => d.Details.Category, "Category.Name"));
		}

		// ---- registration

		[Fact]
		public void RegisteringThePairAgain_ReplacesTheMap()
		{
			var mapper = new Mapper()
				.Register(new MappingConfiguration<Product, ProductLabel>().ForMember(d => d.Name, "Category.Name"))
				.Register(new MappingConfiguration<Product, ProductLabel>());

			Assert.Equal(["Hammer", "Shovel", "Wrench", "Rake"], Map<ProductLabel>(mapper).Select(r => r.Name));
		}

		[Fact]
		public void UnregisteredPair_Throws()
		{
			var exception = Assert.Throws<InvalidOperationException>(() => Map<ProductLabel>(new Mapper()));
			Assert.Contains("No mapping registered", exception.Message);
		}

		[Fact]
		public void ImpossibleConversion_FailsWhenRegistering()
		{
			// int → string has no conversion. The map is built eagerly, so this fails at configuration time.
			Assert.Throws<InvalidOperationException>(() => new Mapper().Register(
				new MappingConfiguration<Product, ProductLabel>().ForMember(d => d.Name, nameof(Product.Quantity))));
		}

		[Fact]
		public void NonSimpleDestination_Throws()
		{
			Assert.Throws<ArgumentException>(() =>
				new MappingConfiguration<Product, ProductLabel>().ForMember(d => d.Name.Length, nameof(Product.Quantity)));
		}

		[Fact]
		public void NullableSourceIntoNonNullableDestination_FailsForNullRows()
		{
			// NullableQuantity is null for row 2. The (int) conversion is translated to SQL, and the null fails
			// when EF materializes the row.
			var mapper = new Mapper().Register(
				new MappingConfiguration<Product, ProductQuantity>().ForMember(d => d.Quantity, nameof(Product.NullableQuantity)));

			Assert.Throws<InvalidOperationException>(() => Map<ProductQuantity>(mapper));
		}

		// ---- composition

		[Fact]
		public void FilterOrderMapPaginate()
		{
			// active: Hammer, Shovel, Rake. By name desc: Shovel, Rake, Hammer.
			using var context = Database.CreateContext();
			var page = context.Products
				.ApplyLogicsFilter([new FilterNode { FieldSelector = nameof(Product.IsActive), FilterCondition = FilterCondition.Equals, SearchValue = true }])
				.ApplyOrder([new OrderParameters(nameof(Product.Name), OrderDirection.Descending)])
				.ApplySelect<Product, ProductSummary>(SummaryMapper())
				.Paginate(1, 2);

			Assert.Equal(["Shovel", "Rake"], page.Select(r => r.Name));
			Assert.Equal(["Garden", "Garden"], page.Select(r => r.CategoryName));
			Assert.Equal(2, page.TotalPages);
		}
	}
}
