using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathCos, Piped = PipedForm.FirstArgument)]
internal record MathCosSelector<T> : FunctionPropertySelector<T>
{
	public MathCosSelector() : base(SelectorDefaults.MathCos) { }
	public MathCosSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathCos, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => MathCosSelector.Table;
}

[SelectorToken(SelectorDefaults.MathCos, Piped = PipedForm.FirstArgument)]
internal record MathCosSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.Numeric], ResultTypes.Fixed(typeof(double)),
			p => target => target.CreateUnaryMathFunction(MethodInfos.MathCosMethodInfo, p[0])),
	];

	public MathCosSelector() : base(SelectorDefaults.MathCos) { }
	public MathCosSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathCos, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
