using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathAtan)]
internal record MathAtanSelector<T> : FunctionPropertySelector<T>
{
	public override Type SelectorType => typeof(double);
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

[SelectorToken(SelectorDefaults.MathAtan)]
internal record MathAtanSelector : FunctionPropertySelector
{
	public override Type SelectorType => typeof(double);
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