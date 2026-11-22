using ExpDataWorker.PropertySelectors.Registration;
using ExpDataWorker.PropertySelectors.SelectorImplementations;

namespace ExpDataWorker.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringStartsWith)]
internal record StringStartsWithSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => typeof(bool);
	public StringStartsWithSelector() : base(SelectorDefaults.StringStartsWith) { }
	public StringStartsWithSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringStartsWith, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			// Overload with one parameter => target.StartsWith(searchValue)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateStringInstanceFunction(MethodInfos.StringStartsWithMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.StringStartsWith)]
internal record StringStartsWithSelector : FunctionPropertySelector
{
	public override Type? SelectorType => typeof(bool);
	public StringStartsWithSelector() : base(SelectorDefaults.StringStartsWith) { }
	public StringStartsWithSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringStartsWith, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			// Overload with one parameter => target.StartsWith(searchValue)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateStringInstanceFunction(MethodInfos.StringStartsWithMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}
