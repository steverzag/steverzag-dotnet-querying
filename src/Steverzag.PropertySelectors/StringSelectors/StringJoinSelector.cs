using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.StringSelectors;

[SelectorToken(SelectorDefaults.StringJoin, Piped = PipedForm.Rejected, PipedHint = StringJoinSelector.PipedHint, Category = FunctionCategory.String)]
internal record StringJoinSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public StringJoinSelector() : base(SelectorDefaults.StringJoin) { }
	public StringJoinSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringJoin, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => StringJoinSelector.Table;
}

[SelectorToken(SelectorDefaults.StringJoin, Piped = PipedForm.Rejected, PipedHint = StringJoinSelector.PipedHint, Category = FunctionCategory.String)]
internal record StringJoinSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal const string PipedHint = "Its first argument is the separator. To join the piped value, pass it as one of the values after the separator.";

	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([ArgumentConstraint.String, ArgumentConstraint.String], ResultTypes.Fixed(typeof(string)),
			p => target => target.CreateStringJoinFunction(p[0], [.. p.Skip(1)]))
		{ Rest = ArgumentConstraint.String },
	];

	public StringJoinSelector() : base(SelectorDefaults.StringJoin) { }
	public StringJoinSelector(List<PropertySelector> parameters) : base(SelectorDefaults.StringJoin, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
