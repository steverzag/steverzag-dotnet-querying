using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateNow)]
internal record DateNowSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => typeof(DateTime);
	public DateNowSelector() : base(SelectorDefaults.DateNow) { }
	public DateNowSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateNow, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 0)
		{
			// Parameterless => DateTime.Now
			var selectorOverload = new FunctionPropertySelectorOverload([],
				target => target.CreateDateTimeStaticAccess("Now"));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.DateNow)]
internal record DateNowSelector : FunctionPropertySelector
{
	public override Type? SelectorType => typeof(DateTime);
	public DateNowSelector() : base(SelectorDefaults.DateNow) { }
	public DateNowSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateNow, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 0)
		{
			// Parameterless => DateTime.Now
			var selectorOverload = new FunctionPropertySelectorOverload([],
				target => target.CreateDateTimeStaticAccess("Now"));
			Overloads.Add(selectorOverload);
		}
	}
}
