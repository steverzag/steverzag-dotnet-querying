using ExpDataWorker.PropertySelectors.SelectorImplementations;

namespace ExpDataWorker.PropertySelectors.ArithmeticSelectors;

internal record MathCosSelector : FunctionPropertySelector
{
	public MathCosSelector() : base(SelectorDefaults.MathCos) { }
	public MathCosSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathCos, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.MathCosMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}