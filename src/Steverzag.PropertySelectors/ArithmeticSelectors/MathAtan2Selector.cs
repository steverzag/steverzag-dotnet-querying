using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathAtan2, Piped = PipedForm.FirstArgument)]
internal record MathAtan2Selector<T> : FunctionPropertySelector<T>
{
	public MathAtan2Selector() : base(SelectorDefaults.MathAtan2) { }
	public MathAtan2Selector(List<PropertySelector> parameters) : base(SelectorDefaults.MathAtan2, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => MathAtan2Selector.Table;
}

[SelectorToken(SelectorDefaults.MathAtan2, Piped = PipedForm.FirstArgument)]
internal record MathAtan2Selector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.Numeric, ArgumentConstraint.Numeric], ResultTypes.Fixed(typeof(double)),
			p => target => target.CreateBinaryMathFunction(MethodInfos.MathAtan2MethodInfo, p[0], p[1])),
	];

	public MathAtan2Selector() : base(SelectorDefaults.MathAtan2) { }
	public MathAtan2Selector(List<PropertySelector> parameters) : base(SelectorDefaults.MathAtan2, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
