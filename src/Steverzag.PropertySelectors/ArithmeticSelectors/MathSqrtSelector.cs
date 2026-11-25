using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathSqrt, Piped = PipedForm.FirstArgument)]
internal record MathSqrtSelector<T> : FunctionPropertySelector<T>
{
	public MathSqrtSelector() : base(SelectorDefaults.MathSqrt) { }
	public MathSqrtSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathSqrt, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => MathSqrtSelector.Table;
}

[SelectorToken(SelectorDefaults.MathSqrt, Piped = PipedForm.FirstArgument)]
internal record MathSqrtSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.Numeric], ResultTypes.Fixed(typeof(double)),
			p => target => target.CreateUnaryMathFunction(MethodInfos.MathSqrtMethodInfo, p[0])),
	];

	public MathSqrtSelector() : base(SelectorDefaults.MathSqrt) { }
	public MathSqrtSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathSqrt, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
