using ExpDataWorker.PropertySelectors.Registration;
using ExpDataWorker.PropertySelectors.SelectorImplementations;

namespace ExpDataWorker.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringContains)]
internal record StringContainsSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => typeof(bool);
	public StringContainsSelector() : base(SelectorDefaults.StringContains) { }
	public StringContainsSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringContains, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			// Overload with one parameter => target.Contains(searchValue)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateStringInstanceFunction(MethodInfos.StringContainsMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.StringContains)]
internal record StringContainsSelector : FunctionPropertySelector
{
	public override Type? SelectorType => typeof(bool);
	public StringContainsSelector() : base(SelectorDefaults.StringContains) { }
	public StringContainsSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringContains, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			// Overload with one parameter => target.Contains(searchValue)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateStringInstanceFunction(MethodInfos.StringContainsMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}
