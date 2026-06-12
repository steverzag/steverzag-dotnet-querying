using ExpDataWorker.PropertySelectors.SelectorImplementations;

namespace ExpDataWorker.PropertySelectors.ArithmeticSelectors;

internal record MathAsinSelector<T> : FunctionPropertySelector<T>
{
	public override Type SelectorType => typeof(double);
	public MathAsinSelector() : base(SelectorDefaults.MathAsin) { }
	public MathAsinSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathAsin, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.MathAsinMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}

internal record MathAsinSelector : FunctionPropertySelector
{
	public override Type SelectorType => typeof(double);
	public MathAsinSelector() : base(SelectorDefaults.MathAsin) { }
	public MathAsinSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathAsin, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.MathAsinMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}