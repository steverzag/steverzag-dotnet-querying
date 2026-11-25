using Steverzag.PropertySelectors.Registration;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringIsNullOrEmpty, Piped = PipedForm.FirstArgument)]
internal record StringIsNullOrEmptySelector<T> : FunctionPropertySelector<T>
{
	public StringIsNullOrEmptySelector() : base(SelectorDefaults.StringIsNullOrEmpty) { }
	public StringIsNullOrEmptySelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringIsNullOrEmpty, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => StringIsNullOrEmptySelector.Table;
}

[SelectorToken(SelectorDefaults.StringIsNullOrEmpty, Piped = PipedForm.FirstArgument)]
internal record StringIsNullOrEmptySelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.String], ResultTypes.Fixed(typeof(bool)),
			p => target => target.CreateStringStaticFunction(MethodInfos.StringIsNullOrEmptyMethodInfo, p[0])),
	];

	public StringIsNullOrEmptySelector() : base(SelectorDefaults.StringIsNullOrEmpty) { }
	public StringIsNullOrEmptySelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringIsNullOrEmpty, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
