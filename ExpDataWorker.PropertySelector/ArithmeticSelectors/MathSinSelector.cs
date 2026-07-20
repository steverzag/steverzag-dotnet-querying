using ExpDataWorker.PropertySelectors.Registration;
using ExpDataWorker.PropertySelectors.SelectorImplementations;

namespace ExpDataWorker.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathSin)]
internal record MathSinSelector<T> : FunctionPropertySelector<T>
{
	public override Type SelectorType => typeof(double);
	public MathSinSelector() : base(SelectorDefaults.MathSin) { }
	public MathSinSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathSin, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.SinMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.MathSin)]
internal record MathSinSelector : FunctionPropertySelector
{
	public override Type? SelectorType => typeof(double);
	public MathSinSelector() : base(SelectorDefaults.MathSin) { }
	public MathSinSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathSin, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.SinMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}