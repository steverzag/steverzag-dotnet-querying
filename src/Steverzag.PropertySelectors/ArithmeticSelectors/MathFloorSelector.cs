using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathFloor, Piped = PipedForm.FirstArgument)]
internal record MathFloorSelector<T> : FunctionPropertySelector<T>
{
	public MathFloorSelector() : base(SelectorDefaults.MathFloor) { }
	public MathFloorSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathFloor, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => MathFloorSelector.Table;
}

[SelectorToken(SelectorDefaults.MathFloor, Piped = PipedForm.FirstArgument)]
internal record MathFloorSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.Numeric], (_, a) => Implementations.MathResultType(MethodInfos.MathFloorMethodInfo, a),
			p => target => target.CreateUnaryMathFunction(MethodInfos.MathFloorMethodInfo, p[0], matchOperandType: true)),
	];

	public MathFloorSelector() : base(SelectorDefaults.MathFloor) { }
	public MathFloorSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathFloor, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
