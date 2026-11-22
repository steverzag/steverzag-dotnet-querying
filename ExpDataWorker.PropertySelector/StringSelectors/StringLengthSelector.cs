using ExpDataWorker.PropertySelectors.Registration;

namespace ExpDataWorker.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringLength)]
internal record StringLengthSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => typeof(int);
	public StringLengthSelector() : base(SelectorDefaults.StringLength) { }
	public StringLengthSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringLength, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 0)
		{
			// Parameterless => target.Length
			var selectorOverload = new FunctionPropertySelectorOverload([],
				target => target.CreateStringLengthAccess());
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.StringLength)]
internal record StringLengthSelector : FunctionPropertySelector
{
	public override Type? SelectorType => typeof(int);
	public StringLengthSelector() : base(SelectorDefaults.StringLength) { }
	public StringLengthSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringLength, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 0)
		{
			// Parameterless => target.Length
			var selectorOverload = new FunctionPropertySelectorOverload([],
				target => target.CreateStringLengthAccess());
			Overloads.Add(selectorOverload);
		}
	}
}
