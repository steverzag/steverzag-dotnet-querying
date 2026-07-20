using ExpDataWorker.PropertySelectors.Registration;
using ExpDataWorker.PropertySelectors.SelectorImplementations;

namespace ExpDataWorker.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathLog10)]
internal record MathLog10Selector<T> : FunctionPropertySelector<T>
{
	public override Type SelectorType => typeof(double);
	public MathLog10Selector() : base(SelectorDefaults.MathLog10) { }
	public MathLog10Selector(List<PropertySelector> parameters) : base(SelectorDefaults.MathLog10, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.MathLog10MethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}


[SelectorToken(SelectorDefaults.MathLog10)]
internal record MathLog10Selector : FunctionPropertySelector
{
	public override Type SelectorType => typeof(double);
	public MathLog10Selector() : base(SelectorDefaults.MathLog10) { }
	public MathLog10Selector(List<PropertySelector> parameters) : base(SelectorDefaults.MathLog10, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.MathLog10MethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}