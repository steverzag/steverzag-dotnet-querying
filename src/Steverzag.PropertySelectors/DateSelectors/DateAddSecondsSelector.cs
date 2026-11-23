using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateAddSeconds)]
internal record DateAddSecondsSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => Parameters.Count > 0 ? Parameters[0]?.Type ?? typeof(DateTime) : typeof(DateTime);
	public DateAddSecondsSelector() : base(SelectorDefaults.DateAddSeconds) { }
	public DateAddSecondsSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddSeconds, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 2)
		{
			// Overload with two parameters => AddSeconds(source, amount)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateDateAddFunction("AddSeconds", Parameters[0], Parameters[1]));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.DateAddSeconds)]
internal record DateAddSecondsSelector : FunctionPropertySelector
{
	public override Type? SelectorType => Parameters.Count > 0 ? Parameters[0]?.Type ?? typeof(DateTime) : typeof(DateTime);
	public DateAddSecondsSelector() : base(SelectorDefaults.DateAddSeconds) { }
	public DateAddSecondsSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddSeconds, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 2)
		{
			// Overload with two parameters => AddSeconds(source, amount)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateDateAddFunction("AddSeconds", Parameters[0], Parameters[1]));
			Overloads.Add(selectorOverload);
		}
	}
}
