using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringIsNullOrEmpty, Piped = PipedForm.FirstArgument)]
internal record StringIsNullOrEmptySelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => typeof(bool);
	public StringIsNullOrEmptySelector() : base(SelectorDefaults.StringIsNullOrEmpty) { }
	public StringIsNullOrEmptySelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringIsNullOrEmpty, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			// Overload with one parameter => string.IsNullOrEmpty(value)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateStringStaticFunction(MethodInfos.StringIsNullOrEmptyMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.StringIsNullOrEmpty, Piped = PipedForm.FirstArgument)]
internal record StringIsNullOrEmptySelector : FunctionPropertySelector
{
	public override Type? SelectorType => typeof(bool);
	public StringIsNullOrEmptySelector() : base(SelectorDefaults.StringIsNullOrEmpty) { }
	public StringIsNullOrEmptySelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringIsNullOrEmpty, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			// Overload with one parameter => string.IsNullOrEmpty(value)
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateStringStaticFunction(MethodInfos.StringIsNullOrEmptyMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}
