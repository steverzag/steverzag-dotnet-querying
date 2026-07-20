using ExpDataWorker.PropertySelectors.Registration;
using ExpDataWorker.PropertySelectors.SelectorImplementations;

namespace ExpDataWorker.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathAcos)]
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

[SelectorToken(SelectorDefaults.MathAcos)]
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