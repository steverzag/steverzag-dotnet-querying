using ExpDataWorker.PropertySelectors.SelectorImplementations;

namespace ExpDataWorker.PropertySelectors.ArithmeticSelectors;

internal record MathSignSelector : FunctionPropertySelector
{
	public MathSignSelector() : base(SelectorDefaults.MathSign) { }
	public MathSignSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathSign, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.MathSignMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}