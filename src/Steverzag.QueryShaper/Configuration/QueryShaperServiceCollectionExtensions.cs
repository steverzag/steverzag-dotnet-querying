using Steverzag.QueryShaper.Configuration.Filtering;
using Steverzag.QueryShaper.Configuration.Mapping;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Steverzag.QueryShaper.Configuration
{
	/// <summary>Registers QueryShaper with dependency injection.</summary>
	public static class QueryShaperServiceCollectionExtensions
	{
		/// <summary>
		/// Registers <see cref="IQueryShaper"/>, <see cref="IMapper"/> and <see cref="IBaseSearchConfiguration"/> as
		/// singletons, filled with the maps and searches of <typeparamref name="TConfiguration"/>.
		/// </summary>
		/// <remarks>
		/// It can be called more than once: every configuration adds to the same maps and searches, and the options
		/// compose in the standard way. Invalid options fail when <see cref="IQueryShaper"/> is first resolved.
		/// </remarks>
		/// <typeparam name="TConfiguration">The configuration whose maps and searches to register.</typeparam>
		/// <param name="services">The service collection.</param>
		/// <param name="configure">Sets the <see cref="QueryShaperOptions"/>, or <see langword="null"/> to keep the defaults.</param>
		/// <returns>The service collection, to chain more calls.</returns>
		/// <exception cref="InvalidOperationException">A map is invalid.</exception>
		/// <exception cref="ArgumentException">A search lookup is invalid.</exception>
		/// <example><code>services.AddQueryShaper&lt;AppQueryShaperConfig&gt;(o => o.MaxPageSize = 50);</code></example>
		public static IServiceCollection AddQueryShaper<TConfiguration>(this IServiceCollection services, Action<QueryShaperOptions>? configure = null)
			where TConfiguration : QueryShaperConfiguration, new()
		{
			var mapper = GetOrCreateSingleton(services, () => new Mapper(), m => services.AddSingleton<IMapper>(m));
			var search = GetOrCreateSingleton(services, () => new BaseSearchConfiguration(), s => services.AddSingleton<IBaseSearchConfiguration>(s));

			var builder = new QueryShaperConfigurationBuilder(mapper, search);
			new TConfiguration().Apply(builder);

			services.AddOptions<QueryShaperOptions>();
			if (configure is not null)
			{
				services.Configure(configure);
			}
			services.TryAddSingleton<IQueryShaper, DefaultQueryShaper>();

			return services;
		}

		private static TInstance GetOrCreateSingleton<TInstance>(
			IServiceCollection services,
			Func<TInstance> factory,
			Action<TInstance> registerContract)
			where TInstance : class
		{
			var existing = services
				.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(TInstance))?
				.ImplementationInstance as TInstance;

			if (existing is not null)
				return existing;

			var instance = factory();
			services.AddSingleton(instance);
			registerContract(instance);
			return instance;
		}
	}
}
