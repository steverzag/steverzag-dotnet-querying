using ExpDataWorker.PropertySelectors.Registration;

namespace ExpDataWorker.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateToday)]
internal record DateTodaySelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => typeof(DateTime);
	public DateTodaySelector() : base(SelectorDefaults.DateToday) { }
	public DateTodaySelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateToday, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 0)
		{
			// Parameterless => DateTime.Today
			var selectorOverload = new FunctionPropertySelectorOverload([],
				target => target.CreateDateTimeStaticAccess("Today"));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.DateToday)]
internal record DateTodaySelector : FunctionPropertySelector
{
	public override Type? SelectorType => typeof(DateTime);
	public DateTodaySelector() : base(SelectorDefaults.DateToday) { }
	public DateTodaySelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateToday, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 0)
		{
			// Parameterless => DateTime.Today
			var selectorOverload = new FunctionPropertySelectorOverload([],
				target => target.CreateDateTimeStaticAccess("Today"));
			Overloads.Add(selectorOverload);
		}
	}
}
