using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringIsNullOrWhiteSpace)]
internal record StringIsNullOrWhiteSpaceSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => typeof(bool);
	public StringIsNullOrWhiteSpaceSelector() : base(SelectorDefaults.StringIsNullOrWhiteSpace) { }
	public StringIsNullOrWhiteSpaceSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringIsNullOrWhiteSpace, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			// Overload with one parameter => string.IsNullOrWhiteSpace(value)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateStringStaticFunction(MethodInfos.StringIsNullOrWhiteSpaceMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.StringIsNullOrWhiteSpace)]
internal record StringIsNullOrWhiteSpaceSelector : FunctionPropertySelector
{
	public override Type? SelectorType => typeof(bool);
	public StringIsNullOrWhiteSpaceSelector() : base(SelectorDefaults.StringIsNullOrWhiteSpace) { }
	public StringIsNullOrWhiteSpaceSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringIsNullOrWhiteSpace, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			// Overload with one parameter => string.IsNullOrWhiteSpace(value)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateStringStaticFunction(MethodInfos.StringIsNullOrWhiteSpaceMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}
