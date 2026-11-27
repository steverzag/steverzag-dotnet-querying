using Microsoft.Extensions.DependencyInjection;
using Steverzag.PropertySelectors;
using Steverzag.QueryShaper.Aggregating;
using Steverzag.QueryShaper.Configuration;
using Steverzag.QueryShaper.Configuration.Filtering;
using Steverzag.QueryShaper.Configuration.Mapping;
using Steverzag.QueryShaper.Filtering;
using Steverzag.QueryShaper.Tests.Infrastructure;
using Steverzag.QueryShaper.Tests.Mapping;

namespace Steverzag.QueryShaper.Tests.Configuration
{
	public class ProductSearchAndMapConfiguration : QueryShaperConfiguration
	{
		protected override void OnConfiguring(QueryShaperConfigurationBuilder builder) => builder
			.AddMap<Product, ProductLabel>(m => m.ForMember(d => d.Name, "Category.Name"))
			.ConfigureSearch<Product>([nameof(Product.Name)]);
	}

	public class CategorySearchConfiguration : QueryShaperConfiguration
	{
		protected override void OnConfiguring(QueryShaperConfigurationBuilder builder) => builder
			.ConfigureSearch<Category>([nameof(Category.Name)]);
	}

	public class ProductCategorySearchConfiguration : QueryShaperConfiguration
	{
		protected override void OnConfiguring(QueryShaperConfigurationBuilder builder) => builder
			.ConfigureSearch<Product>(["Category.Name"]);
	}

	/// <summary>
	/// <c>AddQueryShaper</c>, and the rule that the query extensions use only the configuration they are given
	/// (decision 038: no ambient registry).
	/// </summary>
	public class ConfigurationTests(SqliteDatabaseFixture database) : DatabaseTestBase(database)
	{
		private int[] SearchIds(string value, IBaseSearchConfiguration configuration)
		{
			using var context = Database.CreateContext();
			return context.Products.ApplyBaseFilter(value, configuration).OrderBy(p => p.Id).Select(p => p.Id).ToArray();
		}

		// ---- DI registration

		[Fact]
		public void AddQueryShaper_RegistersConcreteTypesAndContracts_AsOneInstance()
		{
			using var provider = new ServiceCollection().AddQueryShaper<ProductSearchAndMapConfiguration>().BuildServiceProvider();

			Assert.Same(provider.GetRequiredService<Mapper>(), provider.GetRequiredService<IMapper>());
			Assert.Same(provider.GetRequiredService<BaseSearchConfiguration>(), provider.GetRequiredService<IBaseSearchConfiguration>());
		}

		[Fact]
		public void AddQueryShaper_ConfiguresMapsAndSearches()
		{
			using var provider = new ServiceCollection().AddQueryShaper<ProductSearchAndMapConfiguration>().BuildServiceProvider();
			var mapper = provider.GetRequiredService<IMapper>();
			var search = provider.GetRequiredService<IBaseSearchConfiguration>();

			using var context = Database.CreateContext();
			Assert.Equal(["Tools", "Garden", "Tools", "Garden"],
				context.Products.OrderBy(p => p.Id).ApplySelect<Product, ProductLabel>(mapper).Select(l => l.Name).ToArray());
			Assert.Equal([2], SearchIds("Sh", search));
		}

		[Fact]
		public void AddQueryShaper_CalledTwice_MergesIntoTheSameInstances()
		{
			var services = new ServiceCollection()
				.AddQueryShaper<ProductSearchAndMapConfiguration>()
				.AddQueryShaper<CategorySearchConfiguration>();
			using var provider = services.BuildServiceProvider();

			var search = provider.GetRequiredService<IBaseSearchConfiguration>();
			Assert.True(search.HasConfiguration<Product>());
			Assert.True(search.HasConfiguration<Category>());
			Assert.Single(services, d => d.ServiceType == typeof(IBaseSearchConfiguration));
			Assert.Single(services, d => d.ServiceType == typeof(IMapper));
		}

		[Fact]
		public void AddQueryShaper_LaterConfigurationOfTheSameType_Wins()
		{
			using var provider = new ServiceCollection()
				.AddQueryShaper<ProductSearchAndMapConfiguration>()
				.AddQueryShaper<ProductCategorySearchConfiguration>()
				.BuildServiceProvider();

			Assert.Equal([1, 3], SearchIds("Tools", provider.GetRequiredService<IBaseSearchConfiguration>()));
		}

		// ---- no ambient configuration (decision 038)

		[Fact]
		public void TwoContainers_EachKeepTheirOwnConfiguration()
		{
			// Two hosts, two tenants or two parallel tests: registering the second doesn't affect the first.
			using var first = new ServiceCollection().AddQueryShaper<ProductSearchAndMapConfiguration>().BuildServiceProvider();
			using var second = new ServiceCollection().AddQueryShaper<ProductCategorySearchConfiguration>().BuildServiceProvider();

			Assert.Empty(SearchIds("Tools", first.GetRequiredService<IBaseSearchConfiguration>()));
			Assert.Equal([1, 3], SearchIds("Tools", second.GetRequiredService<IBaseSearchConfiguration>()));
		}

		[Fact]
		public void TheConfigurationPassed_IsTheOneUsed_EvenAfterAddQueryShaper()
		{
			new ServiceCollection().AddQueryShaper<ProductCategorySearchConfiguration>();
			var passed = new BaseSearchConfiguration().Configure<Product>([nameof(Product.Name)]);

			Assert.Equal([2], SearchIds("Sh", passed));
		}

		[Fact]
		public void ApplyBaseFilter_WithoutAConfiguration_Throws()
		{
			new ServiceCollection().AddQueryShaper<ProductSearchAndMapConfiguration>();
			using var context = Database.CreateContext();

			var exception = Assert.Throws<ArgumentNullException>(() => context.Products.ApplyBaseFilter("Sh", (IBaseSearchConfiguration)null!));
			Assert.Equal("configuration", exception.ParamName);
		}

		[Fact]
		public void ApplySelect_WithoutAMapper_Throws()
		{
			new ServiceCollection().AddQueryShaper<ProductSearchAndMapConfiguration>();
			using var context = Database.CreateContext();

			var exception = Assert.Throws<ArgumentNullException>(() => context.Products.ApplySelect<Product, ProductLabel>(null!));
			Assert.Equal("mapper", exception.ParamName);
		}
	}
}
