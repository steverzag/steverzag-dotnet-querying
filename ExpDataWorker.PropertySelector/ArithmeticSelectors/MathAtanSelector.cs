using ExpDataWorker.PropertySelectors.SelectorImplementations;

namespace ExpDataWorker.PropertySelectors.ArithmeticSelectors;

internal record MathAtanSelector : FunctionPropertySelector
{
	public MathAtanSelector() : base(SelectorDefaults.MathAtan) { }
	public MathAtanSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathAtan, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.MathAtanMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}