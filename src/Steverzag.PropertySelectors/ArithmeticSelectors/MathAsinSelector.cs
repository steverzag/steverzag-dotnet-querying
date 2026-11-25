using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathAsin, Piped = PipedForm.FirstArgument)]
internal record MathAsinSelector<T> : FunctionPropertySelector<T>
{
	public MathAsinSelector() : base(SelectorDefaults.MathAsin) { }
	public MathAsinSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathAsin, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => MathAsinSelector.Table;
}

[SelectorToken(SelectorDefaults.MathAsin, Piped = PipedForm.FirstArgument)]
internal record MathAsinSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.Numeric], ResultTypes.Fixed(typeof(double)),
			p => target => target.CreateUnaryMathFunction(MethodInfos.MathAsinMethodInfo, p[0])),
	];

	public MathAsinSelector() : base(SelectorDefaults.MathAsin) { }
	public MathAsinSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathAsin, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
