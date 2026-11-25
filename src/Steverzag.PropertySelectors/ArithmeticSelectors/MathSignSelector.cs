using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathSign, Piped = PipedForm.FirstArgument)]
internal record MathSignSelector<T> : FunctionPropertySelector<T>
{
	public MathSignSelector() : base(SelectorDefaults.MathSign) { }
	public MathSignSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathSign, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => MathSignSelector.Table;
}

[SelectorToken(SelectorDefaults.MathSign, Piped = PipedForm.FirstArgument)]
internal record MathSignSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.Numeric], ResultTypes.Fixed(typeof(int)),
			p => target => target.CreateUnaryMathFunction(MethodInfos.MathSignMethodInfo, p[0])),
	];

	public MathSignSelector() : base(SelectorDefaults.MathSign) { }
	public MathSignSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathSign, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
