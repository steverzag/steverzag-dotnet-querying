using System.Linq.Expressions;

namespace Steverzag.QueryShaper.Configuration.Mapping;

/// <summary>
/// The maps registered for each pair of types. <c>AddQueryShaper</c> registers one, filled from <c>AddMap</c>, as
/// <see cref="IMapper"/>.
/// </summary>
public sealed class Mapper : IMapper
{
	private readonly Dictionary<(Type In, Type Out), Expression> _compiledMappers = [];

	/// <summary>Registers a map, replacing any map for the same pair. The map is checked now.</summary>
	/// <typeparam name="TIn">The source type.</typeparam>
	/// <typeparam name="TOut">The destination type.</typeparam>
	/// <param name="configuration">The map.</param>
	/// <returns>This mapper, to chain more calls.</returns>
	/// <exception cref="InvalidOperationException">
	/// A value can't be converted to its destination property, or a value that can be null goes into a property that
	/// can't hold null.
	/// </exception>
	public Mapper Register<TIn, TOut>(MappingConfiguration<TIn, TOut> configuration)
	{
		_compiledMappers[(typeof(TIn), typeof(TOut))] = configuration.CreateMappingExpression();
		return this;
	}

	/// <inheritdoc/>
	public IQueryable<TOut> Map<TIn, TOut>(IQueryable<TIn> source)
	{
		var exp = (Expression<Func<TIn, TOut>>)GetMappingExpression<TIn, TOut>();
		return source.Select(exp);
	}

	private Expression GetMappingExpression<TIn, TOut>()
	{
		var key = (typeof(TIn), typeof(TOut));

		if (!_compiledMappers.TryGetValue(key, out var mapper))
			throw new InvalidOperationException(
				$"No mapping registered for '{typeof(TIn).Name}' -> '{typeof(TOut).Name}'.");

		return mapper;
	}
}