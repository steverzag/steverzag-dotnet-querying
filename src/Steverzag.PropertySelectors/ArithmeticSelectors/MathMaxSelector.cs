using Steverzag.PropertySelectors.Registration;

using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathMax, Piped = PipedForm.FirstArgument)]
internal record MathMaxSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => ArithmeticUtils.GetWiderNumericType([.. Parameters.Select(p => p.SelectorType)]);
	public MathMaxSelector() : base(SelectorDefaults.MathMax) { }
	public MathMaxSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathMax, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 2)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateBinaryMathFunction(MethodInfos.MathMaxMethodInfo, Parameters[0], Parameters[1]));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.MathMax, Piped = PipedForm.FirstArgument)]
internal record MathMaxSelector : FunctionPropertySelector
{
	public override Type? SelectorType => ArithmeticUtils.GetWiderNumericType([.. Parameters.Select(p => p.SelectorType)]);
	public MathMaxSelector() : base(SelectorDefaults.MathMax) { }
	public MathMaxSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathMax, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 2)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateBinaryMathFunction(MethodInfos.MathMaxMethodInfo, Parameters[0], Parameters[1]));
			Overloads.Add(selectorOverload);
		}
	}
}