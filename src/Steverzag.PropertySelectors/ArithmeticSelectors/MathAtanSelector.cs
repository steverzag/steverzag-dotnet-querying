using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathAtan, Piped = PipedForm.FirstArgument)]
internal record MathAtanSelector<T> : FunctionPropertySelector<T>
{
	public MathAtanSelector() : base(SelectorDefaults.MathAtan) { }
	public MathAtanSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathAtan, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => MathAtanSelector.Table;
}

[SelectorToken(SelectorDefaults.MathAtan, Piped = PipedForm.FirstArgument)]
internal record MathAtanSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.Numeric], ResultTypes.Fixed(typeof(double)),
			p => target => target.CreateUnaryMathFunction(MethodInfos.MathAtanMethodInfo, p[0])),
	];

	public MathAtanSelector() : base(SelectorDefaults.MathAtan) { }
	public MathAtanSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathAtan, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
