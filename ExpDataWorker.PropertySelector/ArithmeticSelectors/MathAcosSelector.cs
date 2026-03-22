using ExpDataWorker.PropertySelectors.SelectorImplementations;

namespace ExpDataWorker.PropertySelectors.ArithmeticSelectors;

internal record MathAcosSelector : FunctionPropertySelector
{
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