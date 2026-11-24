using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.NullSelectors;

[SelectorToken(SelectorDefaults.Coalesce)]
internal record CoalesceSelector<T> : FunctionPropertySelector<T>
{
	private Type? _resultType;

	public override Type? SelectorType => _resultType;
	public CoalesceSelector() : base(SelectorDefaults.Coalesce) { }
	public CoalesceSelector(List<PropertySelector> parameters) : base(SelectorDefaults.Coalesce, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 0)
			return;

		_resultType = Implementations.ResolveCoalesceType([typeof(T), .. Parameters.Select(p => p.Type)]);

		var valueParameters = new SortedList<int, Type>();
		for (var i = 0; i < Parameters.Count; i++)
			valueParameters.Add(i, typeof(PropertySelector));

		Overloads.Add(new FunctionPropertySelectorOverload(valueParameters,
			target => target.CreateCoalesceExpression(Parameters)));
	}
}

[SelectorToken(SelectorDefaults.Coalesce)]
internal record CoalesceSelector : FunctionPropertySelector
{
	public CoalesceSelector() : base(SelectorDefaults.Coalesce) { }
	public CoalesceSelector(List<PropertySelector> parameters) : base(SelectorDefaults.Coalesce, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 0)
			return;

		var valueParameters = new SortedList<int, Type>();
		for (var i = 0; i < Parameters.Count; i++)
			valueParameters.Add(i, typeof(PropertySelector));

		Overloads.Add(new FunctionPropertySelectorOverload(valueParameters,
			target => target.CreateCoalesceExpression(Parameters)));
	}
}
