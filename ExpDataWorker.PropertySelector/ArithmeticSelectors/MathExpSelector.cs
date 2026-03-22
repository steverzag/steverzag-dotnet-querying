using ExpDataWorker.PropertySelectors.SelectorImplementations;

namespace ExpDataWorker.PropertySelectors.ArithmeticSelectors;

internal record MathExpSelector : FunctionPropertySelector
{
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