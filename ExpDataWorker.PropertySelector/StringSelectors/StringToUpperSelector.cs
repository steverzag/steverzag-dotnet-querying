using ExpDataWorker.PropertySelectors.Registration;
using ExpDataWorker.PropertySelectors.SelectorImplementations;

namespace ExpDataWorker.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringToUpper)]
internal record StringToUpperSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => typeof(string);
	public StringToUpperSelector() : base(SelectorDefaults.StringToUpper) { }
	public StringToUpperSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringToUpper, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 0)
		{
			// Parameterless => target.ToUpper()
			var selectorOverload = new FunctionPropertySelectorOverload([],
				target => target.CreateStringInstanceFunction(MethodInfos.StringToUpperMethodInfo));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.StringToUpper)]
internal record StringToUpperSelector : FunctionPropertySelector
{
	public override Type? SelectorType => typeof(string);
	public StringToUpperSelector() : base(SelectorDefaults.StringToUpper) { }
	public StringToUpperSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringToUpper, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 0)
		{
			// Parameterless => target.ToUpper()
			var selectorOverload = new FunctionPropertySelectorOverload([],
				target => target.CreateStringInstanceFunction(MethodInfos.StringToUpperMethodInfo));
			Overloads.Add(selectorOverload);
		}
	}
}
