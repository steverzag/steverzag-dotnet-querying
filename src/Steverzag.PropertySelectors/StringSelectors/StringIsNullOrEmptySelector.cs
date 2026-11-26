using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringIsNullOrEmpty, Piped = PipedForm.FirstArgument, Category = FunctionCategory.String)]
internal record StringIsNullOrEmptySelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public StringIsNullOrEmptySelector() : base(SelectorDefaults.StringIsNullOrEmpty) { }
	public StringIsNullOrEmptySelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringIsNullOrEmpty, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => StringIsNullOrEmptySelector.Table;
}

[SelectorToken(SelectorDefaults.StringIsNullOrEmpty, Piped = PipedForm.FirstArgument, Category = FunctionCategory.String)]
internal record StringIsNullOrEmptySelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.String], ResultTypes.Fixed(typeof(bool)),
			p => target => target.CreateStringStaticFunction(MethodInfos.StringIsNullOrEmptyMethodInfo, p[0])),
	];

	public StringIsNullOrEmptySelector() : base(SelectorDefaults.StringIsNullOrEmpty) { }
	public StringIsNullOrEmptySelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringIsNullOrEmpty, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
