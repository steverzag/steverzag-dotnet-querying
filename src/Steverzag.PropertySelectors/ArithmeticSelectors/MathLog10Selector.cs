using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathLog10, Piped = PipedForm.FirstArgument)]
internal record MathLog10Selector<T> : FunctionPropertySelector<T>
{
	public MathLog10Selector() : base(SelectorDefaults.MathLog10) { }
	public MathLog10Selector(List<PropertySelector> parameters) : base(SelectorDefaults.MathLog10, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => MathLog10Selector.Table;
}

[SelectorToken(SelectorDefaults.MathLog10, Piped = PipedForm.FirstArgument)]
internal record MathLog10Selector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.Numeric], ResultTypes.Fixed(typeof(double)),
			p => target => target.CreateUnaryMathFunction(MethodInfos.MathLog10MethodInfo, p[0])),
	];

	public MathLog10Selector() : base(SelectorDefaults.MathLog10) { }
	public MathLog10Selector(List<PropertySelector> parameters) : base(SelectorDefaults.MathLog10, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
