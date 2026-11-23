using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringTrim)]
internal record StringTrimSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => typeof(string);
	public StringTrimSelector() : base(SelectorDefaults.StringTrim) { }
	public StringTrimSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringTrim, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 0)
		{
			// Parameterless => target.Trim()
			var selectorOverload = new FunctionPropertySelectorOverload([],
				target => target.CreateStringInstanceFunction(MethodInfos.StringTrimMethodInfo));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.StringTrim)]
internal record StringTrimSelector : FunctionPropertySelector
{
	public override Type? SelectorType => typeof(string);
	public StringTrimSelector() : base(SelectorDefaults.StringTrim) { }
	public StringTrimSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringTrim, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 0)
		{
			// Parameterless => target.Trim()
			var selectorOverload = new FunctionPropertySelectorOverload([],
				target => target.CreateStringInstanceFunction(MethodInfos.StringTrimMethodInfo));
			Overloads.Add(selectorOverload);
		}
	}
}
