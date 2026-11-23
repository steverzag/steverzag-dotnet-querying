using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathRound)]
internal record MathRoundSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => Parameters[0]?.Type ?? default;
	public MathRoundSelector() : base(SelectorDefaults.MathRound) { }
	public MathRoundSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathRound, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.MathRoundMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
		else if (Parameters.Count == 2)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateBinaryMathFunction(MethodInfos.MathRound_2_MethodInfo, Parameters[0], Parameters[1]));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.MathRound)]
internal record MathRoundSelector : FunctionPropertySelector
{
	public override Type? SelectorType => Parameters[0]?.Type ?? default;
	public MathRoundSelector() : base(SelectorDefaults.MathRound) { }
	public MathRoundSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathRound, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.MathRoundMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
		else if (Parameters.Count == 2)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateBinaryMathFunction(MethodInfos.MathRound_2_MethodInfo, Parameters[0], Parameters[1]));
			Overloads.Add(selectorOverload);
		}
	}
}