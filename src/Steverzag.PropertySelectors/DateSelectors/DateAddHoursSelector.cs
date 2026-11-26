using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateAddHours, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Date)]
internal record DateAddHoursSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public DateAddHoursSelector() : base(SelectorDefaults.DateAddHours) { }
	public DateAddHoursSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddHours, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => DateAddHoursSelector.Table;
}

[SelectorToken(SelectorDefaults.DateAddHours, Piped = PipedForm.FirstArgument, Category = FunctionCategory.Date)]
internal record DateAddHoursSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([Implementations.DateWith("AddHours"), ArgumentConstraint.Numeric], Implementations.DateAddResultType,
			p => target => target.CreateDateAddFunction("AddHours", p[0], p[1])),
	];

	public DateAddHoursSelector() : base(SelectorDefaults.DateAddHours) { }
	public DateAddHoursSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateAddHours, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
