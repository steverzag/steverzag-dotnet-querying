using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringLength)]
internal record StringLengthSelector<T> : FunctionPropertySelector<T>
{
	public StringLengthSelector() : base(SelectorDefaults.StringLength) { }
	public StringLengthSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringLength, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => StringLengthSelector.Table;
}

[SelectorToken(SelectorDefaults.StringLength)]
internal record StringLengthSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], ResultTypes.Fixed(typeof(int)), _ => target => target.CreateStringLengthAccess())
		{ Target = ArgumentConstraint.String },
	];

	public StringLengthSelector() : base(SelectorDefaults.StringLength) { }
	public StringLengthSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringLength, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
