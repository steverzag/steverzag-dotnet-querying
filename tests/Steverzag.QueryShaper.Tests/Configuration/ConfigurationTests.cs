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
	/// <c>AddQueryShaper</c> and the static <c>QueryShaperServices</c> fallback. These tests mutate
	/// process-wide state; they share the database collection, so xUnit runs them one at a time, and each
	/// test registers what it needs first. A "nothing registered" test can't be written reliably for the
	/// same reason (decision D4 in the roadmap).
	/// </summary>
	public class ConfigurationTests(SqliteDatabaseFixture database) : DatabaseTestBase(database)
	{
		private int[] SearchIds(string value, IBaseSearchConfiguration? configuration = null)
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

		// ---- static fallback (QueryShaperServices)

		[Fact]
		public void WithoutAnExplicitConfiguration_TheRegisteredOneIsUsed()
		{
			new ServiceCollection().AddQueryShaper<ProductSearchAndMapConfiguration>();

			Assert.Equal([2], SearchIds("Sh"));

			using var context = Database.CreateContext();
			Assert.Equal(["Tools", "Garden", "Tools", "Garden"],
				context.Products.OrderBy(p => p.Id).ApplySelect<Product, ProductLabel>().Select(l => l.Name).ToArray());
		}

		[Fact]
		public void TheStaticFallback_FollowsTheLastServiceCollection()
		{
			// Two independent containers (two hosts, two tenants, two tests): the static fallback points at
			// whichever called AddQueryShaper last. This is the hazard behind decision D4.
			using var first = new ServiceCollection().AddQueryShaper<ProductSearchAndMapConfiguration>().BuildServiceProvider();
			using var second = new ServiceCollection().AddQueryShaper<ProductCategorySearchConfiguration>().BuildServiceProvider();

			Assert.Equal([1, 3], SearchIds("Tools"));                                                   // the second one's search
			Assert.Empty(SearchIds("Tools", first.GetRequiredService<IBaseSearchConfiguration>()));    // the first is unaffected when passed explicitly
		}

		[Fact]
		public void AnExplicitConfiguration_WinsOverTheStaticFallback()
		{
			new ServiceCollection().AddQueryShaper<ProductCategorySearchConfiguration>();
			var explicitConfiguration = new BaseSearchConfiguration().Configure<Product>([nameof(Product.Name)]);

			Assert.Equal([2], SearchIds("Sh", explicitConfiguration));
		}
	}
}
