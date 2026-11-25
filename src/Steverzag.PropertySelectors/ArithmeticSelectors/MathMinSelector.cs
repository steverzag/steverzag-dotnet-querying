using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathMin, Piped = PipedForm.FirstArgument)]
internal record MathMinSelector<T> : FunctionPropertySelector<T>
{
	public MathMinSelector() : base(SelectorDefaults.MathMin) { }
	public MathMinSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathMin, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => MathMinSelector.Table;
}

[SelectorToken(SelectorDefaults.MathMin, Piped = PipedForm.FirstArgument)]
internal record MathMinSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.Numeric, ArgumentConstraint.Numeric], (_, a) => Implementations.MathResultType(MethodInfos.MathMinMethodInfo, a),
			p => target => target.CreateBinaryMathFunction(MethodInfos.MathMinMethodInfo, p[0], p[1], matchOperandType: true)),
	];

	public MathMinSelector() : base(SelectorDefaults.MathMin) { }
	public MathMinSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathMin, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
