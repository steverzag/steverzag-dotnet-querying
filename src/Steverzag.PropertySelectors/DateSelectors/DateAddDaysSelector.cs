using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateAddDays, Piped = PipedForm.FirstArgument)]
internal record DateAddDaysSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => Parameters.Count > 0 ? Parameters[0].Type : null;
	public DateAddDaysSelector() : base(SelectorDefaults.DateAddDays) { }
	public DateAddDaysSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddDays, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 2)
		{
			// Overload with two parameters => AddDays(source, amount)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateDateAddFunction("AddDays", Parameters[0], Parameters[1]));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.DateAddDays, Piped = PipedForm.FirstArgument)]
internal record DateAddDaysSelector : FunctionPropertySelector
{
	public override Type? SelectorType => Parameters.Count > 0 ? Parameters[0].Type : null;
	public DateAddDaysSelector() : base(SelectorDefaults.DateAddDays) { }
	public DateAddDaysSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddDays, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 2)
		{
			// Overload with two parameters => AddDays(source, amount)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateDateAddFunction("AddDays", Parameters[0], Parameters[1]));
			Overloads.Add(selectorOverload);
		}
	}
}
