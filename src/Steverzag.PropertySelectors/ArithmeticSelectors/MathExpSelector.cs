using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathExp, Piped = PipedForm.FirstArgument)]
internal record MathExpSelector<T> : FunctionPropertySelector<T>
{
	public override Type SelectorType => typeof(double);
	public MathExpSelector() : base(SelectorDefaults.MathExp) { }
	public MathExpSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathExp, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.MathExpMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.MathExp, Piped = PipedForm.FirstArgument)]
internal record MathExpSelector : FunctionPropertySelector
{
	public override Type SelectorType => typeof(double);
	public MathExpSelector() : base(SelectorDefaults.MathExp) { }
	public MathExpSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathExp, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.MathExpMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}