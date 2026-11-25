using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathCeiling, Piped = PipedForm.FirstArgument)]
internal record MathCeilingSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => Implementations.MathResultType(MethodInfos.MathCeilingMethodInfo, Parameters.Take(1));
	public MathCeilingSelector() : base(SelectorDefaults.MathCeiling) { }
	public MathCeilingSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathCeiling, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.MathCeilingMethodInfo, Parameters[0], matchOperandType: true));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.MathCeiling, Piped = PipedForm.FirstArgument)]
internal record MathCeilingSelector : FunctionPropertySelector
{
	public override Type? SelectorType => Implementations.MathResultType(MethodInfos.MathCeilingMethodInfo, Parameters.Take(1));
	public MathCeilingSelector() : base(SelectorDefaults.MathCeiling) { }
	public MathCeilingSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathCeiling, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.MathCeilingMethodInfo, Parameters[0], matchOperandType: true));
			Overloads.Add(selectorOverload);
		}
	}
}