using System.Linq.Expressions;
using Steverzag.QueryShaper.Configuration.Filtering;
using Steverzag.QueryShaper.Configuration.Mapping;

namespace Steverzag.QueryShaper.Configuration
{
	/// <summary>
	/// Registers maps and searches inside <see cref="QueryShaperConfiguration.OnConfiguring"/>. Every configuration
	/// passed to <c>AddQueryShaper</c> adds to the same shared registry.
	/// </summary>
	public sealed class QueryShaperConfigurationBuilder
	{
		internal Mapper Mapper { get; }
		internal BaseSearchConfiguration SearchConfiguration { get; }

		internal QueryShaperConfigurationBuilder(Mapper mapper, BaseSearchConfiguration searchConfiguration)
		{
			Mapper = mapper;
			SearchConfiguration = searchConfiguration;
		}

		/// <summary>Registers a map from <typeparamref name="TIn"/> to <typeparamref name="TOut"/>. The map is checked now, so a bad one fails at startup.</summary>
		/// <typeparam name="TIn">The source type.</typeparam>
		/// <typeparam name="TOut">The destination type.</typeparam>
		/// <param name="configure">Sets the destination properties that don't come from a source property of the same name.</param>
		/// <returns>This builder, to chain more calls.</returns>
		/// <exception cref="InvalidOperationException">A value can't be converted to its destination property, or can be null where the destination can't.</exception>
		public QueryShaperConfigurationBuilder AddMap<TIn, TOut>(Action<MappingConfiguration<TIn, TOut>> configure)
		{
			ArgumentNullException.ThrowIfNull(configure);

			var configuration = new MappingConfiguration<TIn, TOut>();
			configure(configuration);
			Mapper.Register(configuration);
			return this;
		}

		/// <summary>Configures the search for <typeparamref name="T"/> as an expression of the row and the search text.</summary>
		/// <typeparam name="T">The type to search.</typeparam>
		/// <param name="searchExpression">The search, such as <c>(p, s) => p.Name.Contains(s)</c>. It isn't checked.</param>
		/// <returns>This builder, to chain more calls.</returns>
		public QueryShaperConfigurationBuilder ConfigureSearch<T>(Expression<Func<T, string, bool>> searchExpression)
		{
			SearchConfiguration.Configure(searchExpression);
			return this;
		}

		/// <summary>
		/// Configures the search for <typeparamref name="T"/>: a row matches when any lookup contains the search text. The
		/// lookups are checked now, so a bad one fails at startup.
		/// </summary>
		/// <typeparam name="T">The type to search.</typeparam>
		/// <param name="searchLookups">Selectors whose type is <see cref="string"/>, such as <c>Name</c> or <c>Address.City</c>.</param>
		/// <returns>This builder, to chain more calls.</returns>
		/// <exception cref="ArgumentException">A lookup isn't a string, or isn't a valid selector for <typeparamref name="T"/>.</exception>
		public QueryShaperConfigurationBuilder ConfigureSearch<T>(IEnumerable<string> searchLookups)
		{
			SearchConfiguration.Configure<T>(searchLookups);
			return this;
		}
	}
}
