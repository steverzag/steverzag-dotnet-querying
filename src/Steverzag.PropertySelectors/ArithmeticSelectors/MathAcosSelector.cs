using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors;

[SelectorToken(SelectorDefaults.MathAcos, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Math)]
internal record MathAcosSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public MathAcosSelector() : base(SelectorDefaults.MathAcos) { }
	public MathAcosSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathAcos, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => MathAcosSelector.Table;
}

[SelectorToken(SelectorDefaults.MathAcos, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Math)]
internal record MathAcosSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.Numeric], ResultTypes.Fixed(typeof(double)),
			p => target => target.CreateUnaryMathFunction(MethodInfos.MathAcosMethodInfo, p[0])),
	];

	public MathAcosSelector() : base(SelectorDefaults.MathAcos) { }
	public MathAcosSelector(List<PropertySelector> parameters) : base(SelectorDefaults.MathAcos, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
