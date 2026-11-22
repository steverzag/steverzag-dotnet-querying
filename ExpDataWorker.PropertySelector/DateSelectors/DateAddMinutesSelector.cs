using ExpDataWorker.PropertySelectors.Registration;

namespace ExpDataWorker.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateAddMinutes)]
internal record DateAddMinutesSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => Parameters.Count > 0 ? Parameters[0]?.Type ?? typeof(DateTime) : typeof(DateTime);
	public DateAddMinutesSelector() : base(SelectorDefaults.DateAddMinutes) { }
	public DateAddMinutesSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddMinutes, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 2)
		{
			// Overload with two parameters => AddMinutes(source, amount)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateDateAddFunction("AddMinutes", Parameters[0], Parameters[1]));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.DateAddMinutes)]
internal record DateAddMinutesSelector : FunctionPropertySelector
{
	public override Type? SelectorType => Parameters.Count > 0 ? Parameters[0]?.Type ?? typeof(DateTime) : typeof(DateTime);
	public DateAddMinutesSelector() : base(SelectorDefaults.DateAddMinutes) { }
	public DateAddMinutesSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddMinutes, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 2)
		{
			// Overload with two parameters => AddMinutes(source, amount)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateDateAddFunction("AddMinutes", Parameters[0], Parameters[1]));
			Overloads.Add(selectorOverload);
		}
	}
}
