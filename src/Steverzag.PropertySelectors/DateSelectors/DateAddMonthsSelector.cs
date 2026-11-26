using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateAddMonths, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Date)]
internal record DateAddMonthsSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public DateAddMonthsSelector() : base(SelectorDefaults.DateAddMonths) { }
	public DateAddMonthsSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddMonths, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => DateAddMonthsSelector.Table;
}

[SelectorToken(SelectorDefaults.DateAddMonths, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Date)]
internal record DateAddMonthsSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([Implementations.DateWith("AddMonths"), ArgumentConstraint.Numeric], Implementations.DateAddResultType,
			p => target => target.CreateDateAddFunction("AddMonths", p[0], p[1])),
	];

	public DateAddMonthsSelector() : base(SelectorDefaults.DateAddMonths) { }
	public DateAddMonthsSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddMonths, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
