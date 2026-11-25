using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathRound, Piped = PipedForm.FirstArgument)]
internal record MathRoundSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => Implementations.MathResultType(Parameters.Count == 2 ? MethodInfos.MathRound_2_MethodInfo : MethodInfos.MathRoundMethodInfo, Parameters.Take(1));
	public MathRoundSelector() : base(SelectorDefaults.MathRound) { }
	public MathRoundSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathRound, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.MathRoundMethodInfo, Parameters[0], matchOperandType: true));
			Overloads.Add(selectorOverload);
		}
		else if (Parameters.Count == 2)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateBinaryMathFunction(MethodInfos.MathRound_2_MethodInfo, Parameters[0], Parameters[1], matchOperandType: true));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.MathRound, Piped = PipedForm.FirstArgument)]
internal record MathRoundSelector : FunctionPropertySelector
{
	public override Type? SelectorType => Implementations.MathResultType(Parameters.Count == 2 ? MethodInfos.MathRound_2_MethodInfo : MethodInfos.MathRoundMethodInfo, Parameters.Take(1));
	public MathRoundSelector() : base(SelectorDefaults.MathRound) { }
	public MathRoundSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathRound, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.MathRoundMethodInfo, Parameters[0], matchOperandType: true));
			Overloads.Add(selectorOverload);
		}
		else if (Parameters.Count == 2)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateBinaryMathFunction(MethodInfos.MathRound_2_MethodInfo, Parameters[0], Parameters[1], matchOperandType: true));
			Overloads.Add(selectorOverload);
		}
	}
}