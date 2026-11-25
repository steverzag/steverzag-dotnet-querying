using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateAddYears, Piped = PipedForm.FirstArgument)]
internal record DateAddYearsSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => Parameters.Count > 0 ? Parameters[0].Type : null;
	public DateAddYearsSelector() : base(SelectorDefaults.DateAddYears) { }
	public DateAddYearsSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddYears, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 2)
		{
			// Overload with two parameters => AddYears(source, amount)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateDateAddFunction("AddYears", Parameters[0], Parameters[1]));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.DateAddYears, Piped = PipedForm.FirstArgument)]
internal record DateAddYearsSelector : FunctionPropertySelector
{
	public override Type? SelectorType => Parameters.Count > 0 ? Parameters[0].Type : null;
	public DateAddYearsSelector() : base(SelectorDefaults.DateAddYears) { }
	public DateAddYearsSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddYears, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 2)
		{
			// Overload with two parameters => AddYears(source, amount)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateDateAddFunction("AddYears", Parameters[0], Parameters[1]));
			Overloads.Add(selectorOverload);
		}
	}
}
