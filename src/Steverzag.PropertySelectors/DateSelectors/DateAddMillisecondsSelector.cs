using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateAddMilliseconds, Piped = PipedForm.FirstArgument)]
internal record DateAddMillisecondsSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => Parameters.Count > 0 ? Parameters[0]?.Type ?? typeof(DateTime) : typeof(DateTime);
	public DateAddMillisecondsSelector() : base(SelectorDefaults.DateAddMilliseconds) { }
	public DateAddMillisecondsSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddMilliseconds, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 2)
		{
			// Overload with two parameters => AddMilliseconds(source, amount)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateDateAddFunction("AddMilliseconds", Parameters[0], Parameters[1]));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.DateAddMilliseconds, Piped = PipedForm.FirstArgument)]
internal record DateAddMillisecondsSelector : FunctionPropertySelector
{
	public override Type? SelectorType => Parameters.Count > 0 ? Parameters[0]?.Type ?? typeof(DateTime) : typeof(DateTime);
	public DateAddMillisecondsSelector() : base(SelectorDefaults.DateAddMilliseconds) { }
	public DateAddMillisecondsSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddMilliseconds, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 2)
		{
			// Overload with two parameters => AddMilliseconds(source, amount)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateDateAddFunction("AddMilliseconds", Parameters[0], Parameters[1]));
			Overloads.Add(selectorOverload);
		}
	}
}
