using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringSubstring)]
internal record StringSubstringSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => typeof(string);
	public StringSubstringSelector() : base(SelectorDefaults.StringSubstring) { }
	public StringSubstringSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringSubstring, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			// Overload with one parameter => target.Substring(startIndex)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateStringInstanceFunction(MethodInfos.StringSubstringMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
		else if (Parameters.Count == 2)
		{
			// Overload with two parameters => target.Substring(startIndex, length)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateStringInstanceFunction(MethodInfos.StringSubstring_2_MethodInfo, Parameters[0], Parameters[1]));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.StringSubstring)]
internal record StringSubstringSelector : FunctionPropertySelector
{
	public override Type? SelectorType => typeof(string);
	public StringSubstringSelector() : base(SelectorDefaults.StringSubstring) { }
	public StringSubstringSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringSubstring, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			// Overload with one parameter => target.Substring(startIndex)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateStringInstanceFunction(MethodInfos.StringSubstringMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
		else if (Parameters.Count == 2)
		{
			// Overload with two parameters => target.Substring(startIndex, length)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateStringInstanceFunction(MethodInfos.StringSubstring_2_MethodInfo, Parameters[0], Parameters[1]));
			Overloads.Add(selectorOverload);
		}
	}
}
