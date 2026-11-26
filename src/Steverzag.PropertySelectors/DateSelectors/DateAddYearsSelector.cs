using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateAddYears, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Date)]
internal record DateAddYearsSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public DateAddYearsSelector() : base(SelectorDefaults.DateAddYears) { }
	public DateAddYearsSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddYears, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => DateAddYearsSelector.Table;
}

[SelectorToken(SelectorDefaults.DateAddYears, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Date)]
internal record DateAddYearsSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([Implementations.DateWith("AddYears"), ArgumentConstraint.Numeric], Implementations.DateAddResultType,
			p => target => target.CreateDateAddFunction("AddYears", p[0], p[1])),
	];

	public DateAddYearsSelector() : base(SelectorDefaults.DateAddYears) { }
	public DateAddYearsSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddYears, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
