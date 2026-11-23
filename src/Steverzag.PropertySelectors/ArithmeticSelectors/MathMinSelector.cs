using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathMin)]
internal record MathMinSelector<T> : FunctionPropertySelector<T>
{
	public override Type? SelectorType => ArithmeticUtils.GetWiderNumericType([.. Parameters.Select(p => p.SelectorType)]);
	public MathMinSelector() : base(SelectorDefaults.MathMin) { }
	public MathMinSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathMin, parameters) { }
	public override void InitializeOverloads()
	{
		if (Parameters.Count == 2)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateBinaryMathFunction(MethodInfos.MathMinMethodInfo, Parameters[0], Parameters[1]));
			Overloads.Add(selectorOverload);
		}
	}
}

[SelectorToken(SelectorDefaults.MathMin)]
internal record MathMinSelector : FunctionPropertySelector
{
	public override Type? SelectorType => ArithmeticUtils.GetWiderNumericType([.. Parameters.Select(p => p.SelectorType)]);
	public MathMinSelector() : base(SelectorDefaults.MathMin) { }
	public MathMinSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathMin, parameters) { }

	public override void InitializeOverloads()
	{
		if (Parameters.Count == 2)
		{
			var valueParameters = new SortedList<int, Type> { { 0, typeof(PropertySelector) }, { 1, typeof(PropertySelector) } };
			var selectorOverload = new FunctionPropertySelectorOverload(valueParameters,
				target => target.CreateBinaryMathFunction(MethodInfos.MathMinMethodInfo, Parameters[0], Parameters[1]));
			Overloads.Add(selectorOverload);
		}
	}
}