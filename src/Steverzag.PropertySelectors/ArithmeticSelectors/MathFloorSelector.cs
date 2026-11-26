using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathFloor, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Math)]
internal record MathFloorSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public MathFloorSelector() : base(SelectorDefaults.MathFloor) { }
	public MathFloorSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathFloor, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => MathFloorSelector.Table;
}

[SelectorToken(SelectorDefaults.MathFloor, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Math)]
internal record MathFloorSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.Numeric], (_, a) => Implementations.MathResultType(MethodInfos.MathFloorMethodInfo, a),
			p => target => target.CreateUnaryMathFunction(MethodInfos.MathFloorMethodInfo, p[0], matchOperandType: true)),
	];

	public MathFloorSelector() : base(SelectorDefaults.MathFloor) { }
	public MathFloorSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathFloor, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
