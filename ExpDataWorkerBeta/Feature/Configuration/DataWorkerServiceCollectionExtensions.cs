using ExpDataWorkerBeta.Feature.Configuration.Filtering;
using ExpDataWorkerBeta.Feature.Configuration.Mapping;
using Microsoft.Extensions.DependencyInjection;

namespace ExpDataWorkerBeta.Feature.Configuration
{
	public static class DataWorkerServiceCollectionExtensions
	{
		/// <summary>
		/// Builds the supplied <typeparamref name="TConfiguration"/> and contributes its maps and searches
		/// to a single shared <see cref="Mapper"/> and <see cref="BaseSearchConfiguration"/>, registered as
		/// singletons behind both their concrete types and the <see cref="IMapper"/> /
		/// <see cref="IBaseSearchConfiguration"/> contracts.
		/// </summary>
		/// <remarks>
		/// Safe to call multiple times: every configuration merges into the same registry instances rather
		/// than registering duplicate singletons. The built artifacts are also exposed ambiently through
		/// <see cref="DataWorkerServices"/> so the query extensions can resolve them without any extra
		/// initialization call.
		/// </remarks>
		public static IServiceCollection AddDataWorker<TConfiguration>(this IServiceCollection services)
			where TConfiguration : DataWorkerConfiguration, new()
		{
			var mapper = GetOrCreateSingleton(services, () => new Mapper(), m => services.AddSingleton<IMapper>(m));
			var search = GetOrCreateSingleton(services, () => new BaseSearchConfiguration(), s => services.AddSingleton<IBaseSearchConfiguration>(s));

			var builder = new DataWorkerConfigurationBuilder(mapper, search);
			new TConfiguration().Apply(builder);

			DataWorkerServices.Register<IMapper>(mapper);
			DataWorkerServices.Register<IBaseSearchConfiguration>(search);

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
