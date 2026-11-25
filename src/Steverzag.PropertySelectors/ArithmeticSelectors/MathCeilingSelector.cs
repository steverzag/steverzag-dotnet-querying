using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathCeiling, Piped = PipedForm.FirstArgument)]
internal record MathCeilingSelector<T> : FunctionPropertySelector<T>
{
	public MathCeilingSelector() : base(SelectorDefaults.MathCeiling) { }
	public MathCeilingSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathCeiling, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => MathCeilingSelector.Table;
}

[SelectorToken(SelectorDefaults.MathCeiling, Piped = PipedForm.FirstArgument)]
internal record MathCeilingSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.Numeric], (_, a) => Implementations.MathResultType(MethodInfos.MathCeilingMethodInfo, a),
			p => target => target.CreateUnaryMathFunction(MethodInfos.MathCeilingMethodInfo, p[0], matchOperandType: true)),
	];

	public MathCeilingSelector() : base(SelectorDefaults.MathCeiling) { }
	public MathCeilingSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathCeiling, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
