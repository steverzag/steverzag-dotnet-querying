using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringConcat, Piped = PipedForm.FirstArgument, Category = FunctionCategory.String)]
internal record StringConcatSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public StringConcatSelector() : base(SelectorDefaults.StringConcat) { }
	public StringConcatSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringConcat, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => StringConcatSelector.Table;
}

[SelectorToken(SelectorDefaults.StringConcat, Piped = PipedForm.FirstArgument, Category = FunctionCategory.String)]
internal record StringConcatSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.String, ArgumentConstraint.String], ResultTypes.Fixed(typeof(string)),
			p => target => target.CreateStringStaticFunction(MethodInfos.StringConcat_2_MethodInfo, p[0], p[1])),
		new([ArgumentConstraint.String, ArgumentConstraint.String, ArgumentConstraint.String], ResultTypes.Fixed(typeof(string)),
			p => target => target.CreateStringStaticFunction(MethodInfos.StringConcat_3_MethodInfo, p[0], p[1], p[2])),
		new([ArgumentConstraint.String, ArgumentConstraint.String, ArgumentConstraint.String, ArgumentConstraint.String], ResultTypes.Fixed(typeof(string)),
			p => target => target.CreateStringStaticFunction(MethodInfos.StringConcat_4_MethodInfo, p[0], p[1], p[2], p[3])),
	];

	public StringConcatSelector() : base(SelectorDefaults.StringConcat) { }
	public StringConcatSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringConcat, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
