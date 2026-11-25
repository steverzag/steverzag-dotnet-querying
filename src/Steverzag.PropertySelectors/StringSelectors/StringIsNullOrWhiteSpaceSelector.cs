using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringIsNullOrWhiteSpace, Piped = PipedForm.FirstArgument)]
internal record StringIsNullOrWhiteSpaceSelector<T> : FunctionPropertySelector<T>
{
	public StringIsNullOrWhiteSpaceSelector() : base(SelectorDefaults.StringIsNullOrWhiteSpace) { }
	public StringIsNullOrWhiteSpaceSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringIsNullOrWhiteSpace, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => StringIsNullOrWhiteSpaceSelector.Table;
}

[SelectorToken(SelectorDefaults.StringIsNullOrWhiteSpace, Piped = PipedForm.FirstArgument)]
internal record StringIsNullOrWhiteSpaceSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.String], ResultTypes.Fixed(typeof(bool)),
			p => target => target.CreateStringStaticFunction(MethodInfos.StringIsNullOrWhiteSpaceMethodInfo, p[0])),
	];

	public StringIsNullOrWhiteSpaceSelector() : base(SelectorDefaults.StringIsNullOrWhiteSpace) { }
	public StringIsNullOrWhiteSpaceSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringIsNullOrWhiteSpace, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
