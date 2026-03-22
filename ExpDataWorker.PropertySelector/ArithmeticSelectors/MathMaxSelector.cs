
using ExpDataWorker.PropertySelectors.SelectorImplementations;

namespace ExpDataWorker.PropertySelectors.ArithmeticSelectors;

internal record MathMaxSelector : FunctionPropertySelector
{
	public MathMaxSelector() : base(SelectorDefaults.MathMax) { }
	public MathMaxSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathMax, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 2)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateBinaryMathFunction(MethodInfos.MathMaxMethodInfo, Parameters[0], Parameters[1]));
			Overloads.Add(selectorOverload);
		}
	}
}