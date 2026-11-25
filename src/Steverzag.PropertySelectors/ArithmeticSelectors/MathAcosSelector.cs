using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathAcos, Piped = PipedForm.FirstArgument)]
internal record MathAcosSelector<T> : FunctionPropertySelector<T>
{
	public MathAcosSelector() : base(SelectorDefaults.MathAcos) { }
	public MathAcosSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathAcos, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => MathAcosSelector.Table;
}

[SelectorToken(SelectorDefaults.MathAcos, Piped = PipedForm.FirstArgument)]
internal record MathAcosSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.Numeric], ResultTypes.Fixed(typeof(double)),
			p => target => target.CreateUnaryMathFunction(MethodInfos.MathAcosMethodInfo, p[0])),
	];

	public MathAcosSelector() : base(SelectorDefaults.MathAcos) { }
	public MathAcosSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathAcos, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
