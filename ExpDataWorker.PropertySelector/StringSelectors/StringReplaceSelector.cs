using ExpDataWorker.PropertySelectors.Registration;
using ExpDataWorker.PropertySelectors.SelectorImplementations;

namespace ExpDataWorker.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringReplace)]
internal record StringReplaceSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => typeof(string);
	public StringReplaceSelector() : base(SelectorDefaults.StringReplace) { }
	public StringReplaceSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringReplace, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 2)
		{
			// Overload with two parameters => target.Replace(oldValue, newValue)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateStringInstanceFunction(MethodInfos.StringReplaceMethodInfo, Parameters[0], Parameters[1]));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.StringReplace)]
internal record StringReplaceSelector : FunctionPropertySelector
{
	public override Type? SelectorType => typeof(string);
	public StringReplaceSelector() : base(SelectorDefaults.StringReplace) { }
	public StringReplaceSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringReplace, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 2)
		{
			// Overload with two parameters => target.Replace(oldValue, newValue)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateStringInstanceFunction(MethodInfos.StringReplaceMethodInfo, Parameters[0], Parameters[1]));
			Overloads.Add(selectorOverload);
		}
	}
}
