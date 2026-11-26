using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateAddMinutes, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Date)]
internal record DateAddMinutesSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public DateAddMinutesSelector() : base(SelectorDefaults.DateAddMinutes) { }
	public DateAddMinutesSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddMinutes, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => DateAddMinutesSelector.Table;
}

[SelectorToken(SelectorDefaults.DateAddMinutes, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Date)]
internal record DateAddMinutesSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([Implementations.DateWith("AddMinutes"), ArgumentConstraint.Numeric], Implementations.DateAddResultType,
			p => target => target.CreateDateAddFunction("AddMinutes", p[0], p[1])),
	];

	public DateAddMinutesSelector() : base(SelectorDefaults.DateAddMinutes) { }
	public DateAddMinutesSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddMinutes, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
