using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathTruncate, Piped = PipedForm.FirstArgument)]
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

[SelectorToken(SelectorDefaults.MathTruncate, Piped = PipedForm.FirstArgument)]
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