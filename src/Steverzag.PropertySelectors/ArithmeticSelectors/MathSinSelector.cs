using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathSin, Piped = PipedForm.FirstArgument)]
internal record MathSinSelector<T> : FunctionPropertySelector<T>
{
	public MathSinSelector() : base(SelectorDefaults.MathSin) { }
	public MathSinSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathSin, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => MathSinSelector.Table;
}

[SelectorToken(SelectorDefaults.MathSin, Piped = PipedForm.FirstArgument)]
internal record MathSinSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.Numeric], ResultTypes.Fixed(typeof(double)),
			p => target => target.CreateUnaryMathFunction(MethodInfos.SinMethodInfo, p[0])),
	];

	public MathSinSelector() : base(SelectorDefaults.MathSin) { }
	public MathSinSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathSin, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
