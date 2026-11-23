using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathAtan2)]
internal record MathAtan2Selector<T> : FunctionPropertySelector<T>
{
	public override Type SelectorType => typeof(double);
	public MathAtan2Selector() : base(SelectorDefaults.MathAtan2) { }
	public MathAtan2Selector(List<PropertySelector> parameters) : base(SelectorDefaults.MathAtan2, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 2)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateBinaryMathFunction(MethodInfos.MathAtan2MethodInfo, Parameters[0], Parameters[1]));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.MathAtan2)]
internal record MathAtan2Selector : FunctionPropertySelector
{
	public override Type SelectorType => typeof(double);
	public MathAtan2Selector() : base(SelectorDefaults.MathAtan2) { }
	public MathAtan2Selector(List<PropertySelector> parameters) : base(SelectorDefaults.MathAtan2, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 2)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateBinaryMathFunction(MethodInfos.MathAtan2MethodInfo, Parameters[0], Parameters[1]));
			Overloads.Add(selectorOverload);
		}
	}
}