using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathAcos, Piped = PipedForm.FirstArgument)]
internal record MathAcosSelector<T> : FunctionPropertySelector<T>
{
	public override Type SelectorType => typeof(double);
	public MathAcosSelector() : base(SelectorDefaults.MathAcos) { }
	public MathAcosSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathAcos, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.MathAcosMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.MathAcos, Piped = PipedForm.FirstArgument)]
internal record MathAcosSelector : FunctionPropertySelector
{
	public override Type SelectorType => typeof(double);

	public MathAcosSelector() : base(SelectorDefaults.MathAcos) { }
	public MathAcosSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathAcos, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.MathAcosMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}