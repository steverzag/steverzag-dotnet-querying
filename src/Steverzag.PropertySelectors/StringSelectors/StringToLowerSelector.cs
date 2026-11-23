using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringToLower)]
internal record StringToLowerSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => typeof(string);
	public StringToLowerSelector() : base(SelectorDefaults.StringToLower) { }
	public StringToLowerSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringToLower, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 0)
		{
			// Parameterless => target.ToLower()
			var selectorOverload = new FunctionPropertySelectorOverload([],
				target => target.CreateStringInstanceFunction(MethodInfos.StringToLowerMethodInfo));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.StringToLower)]
internal record StringToLowerSelector : FunctionPropertySelector
{
	public override Type? SelectorType => typeof(string);
	public StringToLowerSelector() : base(SelectorDefaults.StringToLower) { }
	public StringToLowerSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringToLower, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 0)
		{
			// Parameterless => target.ToLower()
			var selectorOverload = new FunctionPropertySelectorOverload([],
				target => target.CreateStringInstanceFunction(MethodInfos.StringToLowerMethodInfo));
			Overloads.Add(selectorOverload);
		}
	}
}
