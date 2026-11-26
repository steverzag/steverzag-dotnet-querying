using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathMin, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Math)]
internal record MathMinSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public MathMinSelector() : base(SelectorDefaults.MathMin) { }
	public MathMinSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathMin, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => MathMinSelector.Table;
}

[SelectorToken(SelectorDefaults.MathMin, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Math)]
internal record MathMinSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.Numeric, ArgumentConstraint.Numeric], (_, a) => Implementations.MaxMinResultType(a),
			p => target => target.CreateMaxOrMin(p[0], p[1], max: false)),
	];

	public MathMinSelector() : base(SelectorDefaults.MathMin) { }
	public MathMinSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathMin, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
