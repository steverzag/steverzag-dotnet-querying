using System.Linq.Expressions;
using ExpDataWorkerBeta.Feature.Configuration.Filtering;
using ExpDataWorkerBeta.Feature.Configuration.Mapping;

namespace ExpDataWorkerBeta.Feature.Configuration
{
	/// <summary>
	/// Single entry point used inside <see cref="DataWorkerConfiguration.OnConfiguring"/> to register
	/// object maps and configure global (base) searches. The artifacts it accumulates (a <see cref="Mapper"/>
	/// and a <see cref="BaseSearchConfiguration"/>) are what gets handed to the DI container.
	/// </summary>
	public sealed class DataWorkerConfigurationBuilder
	{
		internal Mapper Mapper { get; }
		internal BaseSearchConfiguration SearchConfiguration { get; }

		/// <summary>
		/// Created against shared <see cref="Mapper"/> and <see cref="BaseSearchConfiguration"/> instances
		/// so that several <see cref="DataWorkerConfiguration"/> classes can contribute to the same registry.
		/// </summary>
		internal DataWorkerConfigurationBuilder(Mapper mapper, BaseSearchConfiguration searchConfiguration)
		{
			Mapper = mapper;
			SearchConfiguration = searchConfiguration;
		}

		/// <summary>
		/// Registers a map from <typeparamref name="TIn"/> to <typeparamref name="TOut"/>.
		/// </summary>
		public DataWorkerConfigurationBuilder AddMap<TIn, TOut>(Action<MappingConfiguration<TIn, TOut>> configure)
		{
			ArgumentNullException.ThrowIfNull(configure);

			var configuration = new MappingConfiguration<TIn, TOut>();
			configure(configuration);
			Mapper.Register(configuration);
			return this;
		}

		/// <summary>
		/// Configures the base search for <typeparamref name="T"/> from an explicit predicate.
		/// </summary>
		public DataWorkerConfigurationBuilder ConfigureSearch<T>(Expression<Func<T, string, bool>> searchExpression)
		{
			SearchConfiguration.Configure(searchExpression);
			return this;
		}

		/// <summary>
		/// Configures the base search for <typeparamref name="T"/> from a set of property lookups.
		/// </summary>
		public DataWorkerConfigurationBuilder ConfigureSearch<T>(IEnumerable<string> searchLookups)
		{
			SearchConfiguration.Configure<T>(searchLookups);
			return this;
		}
	}
}
