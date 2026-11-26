using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathTruncate, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Math)]
internal record MathTruncateSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public MathTruncateSelector() : base(SelectorDefaults.MathTruncate) { }
	public MathTruncateSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathTruncate, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => MathTruncateSelector.Table;
}

[SelectorToken(SelectorDefaults.MathTruncate, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Math)]
internal record MathTruncateSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.Numeric], (_, a) => Implementations.MathResultType(MethodInfos.MathTruncateMethodInfo, a),
			p => target => target.CreateUnaryMathFunction(MethodInfos.MathTruncateMethodInfo, p[0], matchOperandType: true)),
	];

	public MathTruncateSelector() : base(SelectorDefaults.MathTruncate) { }
	public MathTruncateSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathTruncate, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
