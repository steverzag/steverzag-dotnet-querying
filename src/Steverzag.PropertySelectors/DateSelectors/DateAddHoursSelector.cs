using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateAddHours)]
internal record DateAddHoursSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => Parameters.Count > 0 ? Parameters[0]?.Type ?? typeof(DateTime) : typeof(DateTime);
	public DateAddHoursSelector() : base(SelectorDefaults.DateAddHours) { }
	public DateAddHoursSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddHours, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 2)
		{
			// Overload with two parameters => AddHours(source, amount)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateDateAddFunction("AddHours", Parameters[0], Parameters[1]));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.DateAddHours)]
internal record DateAddHoursSelector : FunctionPropertySelector
{
	public override Type? SelectorType => Parameters.Count > 0 ? Parameters[0]?.Type ?? typeof(DateTime) : typeof(DateTime);
	public DateAddHoursSelector() : base(SelectorDefaults.DateAddHours) { }
	public DateAddHoursSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddHours, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 2)
		{
			// Overload with two parameters => AddHours(source, amount)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateDateAddFunction("AddHours", Parameters[0], Parameters[1]));
			Overloads.Add(selectorOverload);
		}
	}
}
