using Steverzag.QueryShaper.Configuration.Filtering;
using Steverzag.QueryShaper.Configuration.Mapping;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Steverzag.QueryShaper.Configuration
{
	public static class QueryShaperServiceCollectionExtensions
	{
		/// <summary>
		/// Builds the supplied <typeparamref name="TConfiguration"/> and contributes its maps and searches
		/// to a single shared <see cref="Mapper"/> and <see cref="BaseSearchConfiguration"/>, registered as
		/// singletons behind both their concrete types and the <see cref="IMapper"/> /
		/// <see cref="IBaseSearchConfiguration"/> contracts.
		/// </summary>
		/// <remarks>
		/// Safe to call multiple times: every configuration merges into the same registry instances rather
		/// than registering duplicate singletons.
		/// </remarks>
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
