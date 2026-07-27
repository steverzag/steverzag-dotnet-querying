using ExpDataWorker.PropertySelectors.Registration;
using ExpDataWorker.PropertySelectors.SelectorImplementations;

namespace ExpDataWorker.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringEndsWith)]
internal record StringEndsWithSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => typeof(bool);
	public StringEndsWithSelector() : base(SelectorDefaults.StringEndsWith) { }
	public StringEndsWithSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringEndsWith, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			// Overload with one parameter => target.EndsWith(searchValue)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateStringInstanceFunction(MethodInfos.StringEndsWithMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.StringEndsWith)]
internal record StringEndsWithSelector : FunctionPropertySelector
{
	public override Type? SelectorType => typeof(bool);
	public StringEndsWithSelector() : base(SelectorDefaults.StringEndsWith) { }
	public StringEndsWithSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringEndsWith, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			// Overload with one parameter => target.EndsWith(searchValue)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateStringInstanceFunction(MethodInfos.StringEndsWithMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}
