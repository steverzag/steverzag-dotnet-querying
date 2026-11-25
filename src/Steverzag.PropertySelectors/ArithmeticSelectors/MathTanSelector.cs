using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathTan, Piped = PipedForm.FirstArgument)]
internal record MathTanSelector<T> : FunctionPropertySelector<T>
{
	public override Type SelectorType => typeof(double);
	public MathTanSelector() : base(SelectorDefaults.MathTan) { }
	public MathTanSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathTan, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.MathTanMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.MathTan, Piped = PipedForm.FirstArgument)]
internal record MathTanSelector : FunctionPropertySelector
{
	public override Type? SelectorType => typeof(double);
	public MathTanSelector() : base(SelectorDefaults.MathTan) { }
	public MathTanSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathTan, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.MathTanMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}