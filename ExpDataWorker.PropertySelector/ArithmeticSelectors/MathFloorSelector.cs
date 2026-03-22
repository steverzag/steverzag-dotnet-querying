using ExpDataWorker.PropertySelectors.SelectorImplementations;

namespace ExpDataWorker.PropertySelectors.ArithmeticSelectors;

internal record MathFloorSelector : FunctionPropertySelector
{
	public MathFloorSelector() : base(SelectorDefaults.MathFloor) { }
	public MathFloorSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathFloor, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 1)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters, 
				target => target.CreateUnaryMathFunction(MethodInfos.MathFloorMethodInfo, Parameters[0]));
			Overloads.Add(selectorOverload);
		}
	}
}