using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringIsNullOrWhiteSpace, Piped = PipedForm.FirstArgument, Category = FunctionCategory.String)]
internal record StringIsNullOrWhiteSpaceSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public StringIsNullOrWhiteSpaceSelector() : base(SelectorDefaults.StringIsNullOrWhiteSpace) { }
	public StringIsNullOrWhiteSpaceSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringIsNullOrWhiteSpace, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => StringIsNullOrWhiteSpaceSelector.Table;
}

[SelectorToken(SelectorDefaults.StringIsNullOrWhiteSpace, Piped = PipedForm.FirstArgument, Category = FunctionCategory.String)]
internal record StringIsNullOrWhiteSpaceSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.String], ResultTypes.Fixed(typeof(bool)),
			p => target => target.CreateStringStaticFunction(MethodInfos.StringIsNullOrWhiteSpaceMethodInfo, p[0])),
	];

	public StringIsNullOrWhiteSpaceSelector() : base(SelectorDefaults.StringIsNullOrWhiteSpace) { }
	public StringIsNullOrWhiteSpaceSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringIsNullOrWhiteSpace, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
