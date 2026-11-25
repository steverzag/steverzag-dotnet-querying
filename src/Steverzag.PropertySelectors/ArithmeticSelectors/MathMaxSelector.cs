using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathMax, Piped = PipedForm.FirstArgument)]
internal record MathMaxSelector<T> : FunctionPropertySelector<T>
{
	public MathMaxSelector() : base(SelectorDefaults.MathMax) { }
	public MathMaxSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathMax, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => MathMaxSelector.Table;
}

[SelectorToken(SelectorDefaults.MathMax, Piped = PipedForm.FirstArgument)]
internal record MathMaxSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.Numeric, ArgumentConstraint.Numeric], (_, a) => Implementations.MathResultType(MethodInfos.MathMaxMethodInfo, a),
			p => target => target.CreateBinaryMathFunction(MethodInfos.MathMaxMethodInfo, p[0], p[1], matchOperandType: true)),
	];

	public MathMaxSelector() : base(SelectorDefaults.MathMax) { }
	public MathMaxSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathMax, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
