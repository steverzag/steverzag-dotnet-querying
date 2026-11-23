using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringCompare)]
internal record StringCompareSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => typeof(int);
	public StringCompareSelector() : base(SelectorDefaults.StringCompare) { }
	public StringCompareSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringCompare, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 2)
		{
			// Overload with two parameters => string.Compare(strA, strB)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateStringStaticFunction(MethodInfos.StringCompareMethodInfo, Parameters[0], Parameters[1]));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.StringCompare)]
internal record StringCompareSelector : FunctionPropertySelector
{
	public override Type? SelectorType => typeof(int);
	public StringCompareSelector() : base(SelectorDefaults.StringCompare) { }
	public StringCompareSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringCompare, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 2)
		{
			// Overload with two parameters => string.Compare(strA, strB)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateStringStaticFunction(MethodInfos.StringCompareMethodInfo, Parameters[0], Parameters[1]));
			Overloads.Add(selectorOverload);
		}
	}
}
