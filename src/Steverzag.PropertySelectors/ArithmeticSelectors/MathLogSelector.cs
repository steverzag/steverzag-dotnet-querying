using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathLog, Piped = PipedForm.FirstArgument)]
internal record MathLogSelector<T> : FunctionPropertySelector<T>
{
	public MathLogSelector() : base(SelectorDefaults.MathLog) { }
	public MathLogSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathLog, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => MathLogSelector.Table;
}

[SelectorToken(SelectorDefaults.MathLog, Piped = PipedForm.FirstArgument)]
internal record MathLogSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.Numeric], ResultTypes.Fixed(typeof(double)),
			p => target => target.CreateUnaryMathFunction(MethodInfos.MathLogMethodInfo, p[0])),
	];

	public MathLogSelector() : base(SelectorDefaults.MathLog) { }
	public MathLogSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathLog, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
