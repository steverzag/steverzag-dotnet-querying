using System.Linq.Expressions;

namespace Steverzag.QueryShaper.Configuration.Mapping;

public class Mapper : IMapper
{
	private readonly Dictionary<(Type In, Type Out), Expression> _compiledMappers = [];

	public Mapper Register<TIn, TOut>(MappingConfiguration<TIn, TOut> configuration)
	{
		_compiledMappers[(typeof(TIn), typeof(TOut))] = configuration.CreateMappingExpression();
		return this;
	}

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