using ExpDataWorker.PropertySelectors.Registration;
using ExpDataWorker.PropertySelectors.SelectorImplementations;

namespace ExpDataWorker.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathLog)]
internal record MathLogSelector<T> : FunctionPropertySelector<T>
{
	public override Type SelectorType => typeof(double);
	public MathLogSelector() : base(SelectorDefaults.MathLog) { }
	public MathLogSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathLog, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.MathLogMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.MathLog)]
internal record MathLogSelector : FunctionPropertySelector
{
	public override Type SelectorType => typeof(double);
	public MathLogSelector() : base(SelectorDefaults.MathLog) { }
	public MathLogSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathLog, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.MathLogMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}