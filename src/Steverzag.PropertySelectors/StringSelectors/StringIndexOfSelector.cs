using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringIndexOf)]
internal record StringIndexOfSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => typeof(int);
	public StringIndexOfSelector() : base(SelectorDefaults.StringIndexOf) { }
	public StringIndexOfSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringIndexOf, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			// Overload with one parameter => target.IndexOf(searchValue)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateStringInstanceFunction(MethodInfos.StringIndexOfMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.StringIndexOf)]
internal record StringIndexOfSelector : FunctionPropertySelector
{
	public override Type? SelectorType => typeof(int);
	public StringIndexOfSelector() : base(SelectorDefaults.StringIndexOf) { }
	public StringIndexOfSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringIndexOf, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			// Overload with one parameter => target.IndexOf(searchValue)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateStringInstanceFunction(MethodInfos.StringIndexOfMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}
