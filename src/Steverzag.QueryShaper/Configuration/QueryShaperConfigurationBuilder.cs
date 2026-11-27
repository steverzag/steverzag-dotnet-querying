using System.Linq.Expressions;
using Steverzag.QueryShaper.Configuration.Filtering;
using Steverzag.QueryShaper.Configuration.Mapping;
using Steverzag.QueryShaper.Configuration.Ordering;
using Steverzag.QueryShaper.Ordering;

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
		internal DefaultOrderConfiguration DefaultOrders { get; }

		internal QueryShaperConfigurationBuilder(Mapper mapper, BaseSearchConfiguration searchConfiguration, DefaultOrderConfiguration defaultOrders)
		{
			Mapper = mapper;
			SearchConfiguration = searchConfiguration;
			DefaultOrders = defaultOrders;
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
		/// Configures the search for <typeparamref name="T"/>: a row matches when any lookup contains the search text,
		/// ignoring case unless <paramref name="caseSensitive"/> is set. The lookups are checked now, so a bad one fails at startup.
		/// </summary>
		/// <remarks>See <see cref="BaseSearchConfiguration.Configure{T}(IEnumerable{string}, bool)"/> for how case is ignored.</remarks>
		/// <typeparam name="T">The type to search.</typeparam>
		/// <param name="searchLookups">Selectors whose type is <see cref="string"/>, such as <c>Name</c> or <c>Address.City</c>.</param>
		/// <param name="caseSensitive"><see langword="true"/> to match only the text as written.</param>
		/// <returns>This builder, to chain more calls.</returns>
		/// <exception cref="ArgumentException">A lookup isn't a string, or isn't a valid selector for <typeparamref name="T"/>.</exception>
		public QueryShaperConfigurationBuilder ConfigureSearch<T>(IEnumerable<string> searchLookups, bool caseSensitive = false)
		{
			SearchConfiguration.Configure<T>(searchLookups, caseSensitive);
			return this;
		}

		/// <summary>
		/// Configures the order <see cref="IQueryShaper"/> gives <typeparamref name="T"/>: used on its own when a request has
		/// no order, and added after a request's order as a tie-breaker, so pages are stable. The fields are checked now,
		/// so a bad one fails at startup. It replaces any default order configured for <typeparamref name="T"/>.
		/// </summary>
		/// <remarks>
		/// End it with a unique key, such as <c>Id</c>, so no two rows tie. It's server configuration: the
		/// <see cref="QueryShaperOptions.MemberFilter"/> and <see cref="QueryShaperOptions.MaxOrders"/> don't apply to it. An
		/// entry the request already orders by isn't repeated.
		/// </remarks>
		/// <typeparam name="T">The type to order.</typeparam>
		/// <param name="order">The orders, the first as the primary sort.</param>
		/// <returns>This builder, to chain more calls.</returns>
		/// <exception cref="ArgumentException">The order is empty, contains <see langword="null"/>, or a field isn't a valid selector for <typeparamref name="T"/>.</exception>
		/// <example><code>b.ConfigureDefaultOrder&lt;Product&gt;([new("CreatedAt", OrderDirection.Descending), new("Id")]);</code></example>
		public QueryShaperConfigurationBuilder ConfigureDefaultOrder<T>(IEnumerable<OrderParameters> order)
		{
			DefaultOrders.Configure<T>(order);
			return this;
		}
	}
}
