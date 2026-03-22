using ExpDataWorker.PropertySelectors.SelectorImplementations;

namespace ExpDataWorker.PropertySelectors.ArithmeticSelectors;

internal record MathLogSelector : FunctionPropertySelector
{
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