using ExpDataWorker.PropertySelectors.Registration;
using ExpDataWorker.PropertySelectors.SelectorImplementations;

namespace ExpDataWorker.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathExp)]
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

[SelectorToken(SelectorDefaults.MathExp)]
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