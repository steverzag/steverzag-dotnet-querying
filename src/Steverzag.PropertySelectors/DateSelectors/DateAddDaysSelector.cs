using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateAddDays, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Date)]
internal record DateAddDaysSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public DateAddDaysSelector() : base(SelectorDefaults.DateAddDays) { }
	public DateAddDaysSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddDays, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => DateAddDaysSelector.Table;
}

[SelectorToken(SelectorDefaults.DateAddDays, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Date)]
internal record DateAddDaysSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([Implementations.DateWith("AddDays"), ArgumentConstraint.Numeric], Implementations.DateAddResultType,
			p => target => target.CreateDateAddFunction("AddDays", p[0], p[1])),
	];

	public DateAddDaysSelector() : base(SelectorDefaults.DateAddDays) { }
	public DateAddDaysSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddDays, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
