using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathMax, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Math)]
internal record MathMaxSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public MathMaxSelector() : base(SelectorDefaults.MathMax) { }
	public MathMaxSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathMax, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => MathMaxSelector.Table;
}

[SelectorToken(SelectorDefaults.MathMax, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Math)]
internal record MathMaxSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.Numeric, ArgumentConstraint.Numeric], (_, a) => Implementations.MaxMinResultType(a),
			p => target => target.CreateMaxOrMin(p[0], p[1], max: true)),
	];

	public MathMaxSelector() : base(SelectorDefaults.MathMax) { }
	public MathMaxSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathMax, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
