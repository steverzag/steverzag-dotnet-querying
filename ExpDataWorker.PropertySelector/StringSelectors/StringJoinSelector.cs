using ExpDataWorker.PropertySelectors.Registration;

namespace ExpDataWorker.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringJoin)]
internal record StringJoinSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => typeof(string);
	public StringJoinSelector() : base(SelectorDefaults.StringJoin) { }
	public StringJoinSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringJoin, parameters) { }
	public override void InitializeOverloads()
	{
		// Variadic => string.Join(separator, value0, value1, …): a separator plus at least one value.
		if (Parameters.Count >= 2)
		{
			var valueParameters = StringJoinSelector.CreateValueParameters(Parameters.Count);
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateStringJoinFunction(Parameters[0], [.. Parameters.Skip(1)]));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.StringJoin)]
internal record StringJoinSelector : FunctionPropertySelector
{
	public override Type? SelectorType => typeof(string);
	public StringJoinSelector() : base(SelectorDefaults.StringJoin) { }
	public StringJoinSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringJoin, parameters) { }

	public override void InitializeOverloads()
	{
		// Variadic => string.Join(separator, value0, value1, …): a separator plus at least one value.
		if (Parameters.Count >= 2)
		{
			var valueParameters = CreateValueParameters(Parameters.Count);
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateStringJoinFunction(Parameters[0], [.. Parameters.Skip(1)]));
			Overloads.Add(selectorOverload);
		}
	}

	/// <summary>
	/// Join takes any number of values, so the overload is described for the parameter count
	/// actually parsed instead of registering one overload per supported arity.
	/// </summary>
	internal static SortedList<int, Type> CreateValueParameters(int count)
	{
		var valueParameters = new SortedList<int, Type>(count);
		for (var i = 0; i < count; i++)
		{
			valueParameters.Add(i, typeof(PropertySelector));
		}

		return valueParameters;
	}
}
