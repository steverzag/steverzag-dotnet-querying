using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathRound, Piped = PipedForm.FirstArgument)]
internal record MathRoundSelector<T> : FunctionPropertySelector<T>
{
	public MathRoundSelector() : base(SelectorDefaults.MathRound) { }
	public MathRoundSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathRound, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => MathRoundSelector.Table;
}

[SelectorToken(SelectorDefaults.MathRound, Piped = PipedForm.FirstArgument)]
internal record MathRoundSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.Numeric], (_, a) => Implementations.MathResultType(MethodInfos.MathRoundMethodInfo, a),
			p => target => target.CreateUnaryMathFunction(MethodInfos.MathRoundMethodInfo, p[0], matchOperandType: true)),
		new([ArgumentConstraint.Numeric, ArgumentConstraint.Integer], (_, a) => Implementations.MathResultType(MethodInfos.MathRound_2_MethodInfo, a.Take(1)),
			p => target => target.CreateBinaryMathFunction(MethodInfos.MathRound_2_MethodInfo, p[0], p[1], matchOperandType: true)),
	];

	public MathRoundSelector() : base(SelectorDefaults.MathRound) { }
	public MathRoundSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathRound, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
