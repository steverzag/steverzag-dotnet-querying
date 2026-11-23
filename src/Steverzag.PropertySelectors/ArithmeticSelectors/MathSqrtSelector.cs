using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathSqrt)]
internal record MathSqrtSelector<T> : FunctionPropertySelector<T>
{
	public override Type SelectorType => typeof(double);
	public MathSqrtSelector() : base(SelectorDefaults.MathSqrt) { }
	public MathSqrtSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathSqrt, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.MathSqrtMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.MathSqrt)]
internal record MathSqrtSelector : FunctionPropertySelector
{
	public override Type? SelectorType => typeof(double);
	public MathSqrtSelector() : base(SelectorDefaults.MathSqrt) { }
	public MathSqrtSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathSqrt, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateUnaryMathFunction(MethodInfos.MathSqrtMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}