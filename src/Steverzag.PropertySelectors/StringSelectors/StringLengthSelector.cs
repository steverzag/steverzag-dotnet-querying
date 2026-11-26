using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringLength, Category = FunctionCategory.String)]
internal record StringLengthSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public StringLengthSelector() : base(SelectorDefaults.StringLength) { }
	public StringLengthSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringLength, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => StringLengthSelector.Table;
}

[SelectorToken(SelectorDefaults.StringLength, Category = FunctionCategory.String)]
internal record StringLengthSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], ResultTypes.Fixed(typeof(int)), _ => target => target.CreateStringLengthAccess())
		{ Target = ArgumentConstraint.String },
	];

	public StringLengthSelector() : base(SelectorDefaults.StringLength) { }
	public StringLengthSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringLength, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
