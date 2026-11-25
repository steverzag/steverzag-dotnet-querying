using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathSign, Piped = PipedForm.FirstArgument)]
internal record MathSignSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => typeof(int);
	public MathSignSelector() : base(SelectorDefaults.MathSign) { }
	public MathSignSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathSign, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.MathSignMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.MathSign, Piped = PipedForm.FirstArgument)]
internal record MathSignSelector : FunctionPropertySelector
{
	public override Type? SelectorType => typeof(int);
	public MathSignSelector() : base(SelectorDefaults.MathSign) { }
	public MathSignSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathSign, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.MathSignMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}