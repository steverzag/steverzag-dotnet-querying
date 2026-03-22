using ExpDataWorker.PropertySelectors.SelectorImplementations;

namespace ExpDataWorker.PropertySelectors.ArithmeticSelectors;

internal record MathTruncateSelector : FunctionPropertySelector
{
	public MathTruncateSelector() : base(SelectorDefaults.MathTruncate) { }
	public MathTruncateSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathTruncate, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.MathTruncateMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}