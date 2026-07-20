using ExpDataWorker.PropertySelectors.Registration;
using ExpDataWorker.PropertySelectors.SelectorImplementations;

namespace ExpDataWorker.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathCeiling)]
internal record MathCeilingSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => Parameters[0]?.Type ?? default;
	public MathCeilingSelector() : base(SelectorDefaults.MathCeiling) { }
	public MathCeilingSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathCeiling, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.MathCeilingMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.MathCeiling)]
internal record MathCeilingSelector : FunctionPropertySelector
{
	public override Type? SelectorType => Parameters[0]?.Type ?? default;
	public MathCeilingSelector() : base(SelectorDefaults.MathCeiling) { }
	public MathCeilingSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathCeiling, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.MathCeilingMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}