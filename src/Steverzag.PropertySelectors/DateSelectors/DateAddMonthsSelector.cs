using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateAddMonths, Piped = PipedForm.FirstArgument)]
internal record DateAddMonthsSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => Parameters.Count > 0 ? Parameters[0]?.Type ?? typeof(DateTime) : typeof(DateTime);
	public DateAddMonthsSelector() : base(SelectorDefaults.DateAddMonths) { }
	public DateAddMonthsSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddMonths, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 2)
		{
			// Overload with two parameters => AddMonths(source, amount)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateDateAddFunction("AddMonths", Parameters[0], Parameters[1]));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.DateAddMonths, Piped = PipedForm.FirstArgument)]
internal record DateAddMonthsSelector : FunctionPropertySelector
{
	public override Type? SelectorType => Parameters.Count > 0 ? Parameters[0]?.Type ?? typeof(DateTime) : typeof(DateTime);
	public DateAddMonthsSelector() : base(SelectorDefaults.DateAddMonths) { }
	public DateAddMonthsSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddMonths, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 2)
		{
			// Overload with two parameters => AddMonths(source, amount)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateDateAddFunction("AddMonths", Parameters[0], Parameters[1]));
			Overloads.Add(selectorOverload);
		}
	}
}
