using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathCos, Piped = PipedForm.FirstArgument)]
internal record MathCosSelector<T> : FunctionPropertySelector<T>
{
	public override Type SelectorType => typeof(double);
	public MathCosSelector() : base(SelectorDefaults.MathCos) { }
	public MathCosSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathCos, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.MathCosMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.MathCos, Piped = PipedForm.FirstArgument)]
internal record MathCosSelector : FunctionPropertySelector
{
	public override Type SelectorType => typeof(double);
	public MathCosSelector() : base(SelectorDefaults.MathCos) { }
	public MathCosSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathCos, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.MathCosMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}