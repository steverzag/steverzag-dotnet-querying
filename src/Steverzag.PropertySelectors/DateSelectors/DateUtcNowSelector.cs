using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateUtcNow, Piped = PipedForm.Rejected)]
internal record DateUtcNowSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => typeof(DateTime);
	public DateUtcNowSelector() : base(SelectorDefaults.DateUtcNow) { }
	public DateUtcNowSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateUtcNow, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 0)
		{
			// Parameterless => DateTime.UtcNow
			var selectorOverload = new FunctionPropertySelectorOverload([],
				target => target.CreateDateTimeStaticAccess("UtcNow"));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.DateUtcNow, Piped = PipedForm.Rejected)]
internal record DateUtcNowSelector : FunctionPropertySelector
{
	public override Type? SelectorType => typeof(DateTime);
	public DateUtcNowSelector() : base(SelectorDefaults.DateUtcNow) { }
	public DateUtcNowSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateUtcNow, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 0)
		{
			// Parameterless => DateTime.UtcNow
			var selectorOverload = new FunctionPropertySelectorOverload([],
				target => target.CreateDateTimeStaticAccess("UtcNow"));
			Overloads.Add(selectorOverload);
		}
	}
}
