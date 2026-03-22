using ExpDataWorker.PropertySelectors.SelectorImplementations;

namespace ExpDataWorker.PropertySelectors.ArithmeticSelectors;

internal record MathCeilingSelector : FunctionPropertySelector
{
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