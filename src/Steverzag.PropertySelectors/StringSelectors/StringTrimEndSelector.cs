using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringTrimEnd)]
internal record StringTrimEndSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => typeof(string);
	public StringTrimEndSelector() : base(SelectorDefaults.StringTrimEnd) { }
	public StringTrimEndSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringTrimEnd, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 0)
		{
			// Parameterless => target.TrimEnd()
			var selectorOverload = new FunctionPropertySelectorOverload([],
				target => target.CreateStringInstanceFunction(MethodInfos.StringTrimEndMethodInfo));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.StringTrimEnd)]
internal record StringTrimEndSelector : FunctionPropertySelector
{
	public override Type? SelectorType => typeof(string);
	public StringTrimEndSelector() : base(SelectorDefaults.StringTrimEnd) { }
	public StringTrimEndSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringTrimEnd, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 0)
		{
			// Parameterless => target.TrimEnd()
			var selectorOverload = new FunctionPropertySelectorOverload([],
				target => target.CreateStringInstanceFunction(MethodInfos.StringTrimEndMethodInfo));
			Overloads.Add(selectorOverload);
		}
	}
}
