using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringTrimStart)]
internal record StringTrimStartSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => typeof(string);
	public StringTrimStartSelector() : base(SelectorDefaults.StringTrimStart) { }
	public StringTrimStartSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringTrimStart, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 0)
		{
			// Parameterless => target.TrimStart()
			var selectorOverload = new FunctionPropertySelectorOverload([],
				target => target.CreateStringInstanceFunction(MethodInfos.StringTrimStartMethodInfo));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.StringTrimStart)]
internal record StringTrimStartSelector : FunctionPropertySelector
{
	public override Type? SelectorType => typeof(string);
	public StringTrimStartSelector() : base(SelectorDefaults.StringTrimStart) { }
	public StringTrimStartSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringTrimStart, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 0)
		{
			// Parameterless => target.TrimStart()
			var selectorOverload = new FunctionPropertySelectorOverload([],
				target => target.CreateStringInstanceFunction(MethodInfos.StringTrimStartMethodInfo));
			Overloads.Add(selectorOverload);
		}
	}
}
