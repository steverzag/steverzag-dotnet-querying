using ExpDataWorker.PropertySelectors.Registration;
using ExpDataWorker.PropertySelectors.SelectorImplementations;

namespace ExpDataWorker.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathTruncate)]
internal record MathTruncateSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => Parameters[0]?.Type ?? default;
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

[SelectorToken(SelectorDefaults.MathTruncate)]
internal record MathTruncateSelector : FunctionPropertySelector
{
	public override Type? SelectorType => Parameters[0]?.Type ?? default;
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