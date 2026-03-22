using ExpDataWorker.PropertySelectors.SelectorImplementations;

namespace ExpDataWorker.PropertySelectors.ArithmeticSelectors;

internal record MathTanSelector : FunctionPropertySelector
{
	public MathTanSelector() : base(SelectorDefaults.MathTan) { }
	public MathTanSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathTan, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.MathTanMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}