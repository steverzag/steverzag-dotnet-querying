using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateToday, Piped = PipedForm.Rejected, Category = FunctionCategory.Date)]
internal record DateTodaySelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public DateTodaySelector() : base(SelectorDefaults.DateToday) { }
	public DateTodaySelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateToday, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => DateTodaySelector.Table;
}

[SelectorToken(SelectorDefaults.DateToday, Piped = PipedForm.Rejected, Category = FunctionCategory.Date)]
internal record DateTodaySelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], ResultTypes.Fixed(typeof(DateTime)), _ => target => target.CreateDateTimeStaticAccess("Today")),
	];

	public DateTodaySelector() : base(SelectorDefaults.DateToday) { }
	public DateTodaySelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateToday, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
