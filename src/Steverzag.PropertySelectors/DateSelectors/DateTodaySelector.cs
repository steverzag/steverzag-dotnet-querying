using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateToday, Piped = PipedForm.Rejected)]
internal record DateTodaySelector<T> : FunctionPropertySelector<T>
{
	public DateTodaySelector() : base(SelectorDefaults.DateToday) { }
	public DateTodaySelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateToday, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => DateTodaySelector.Table;
}

[SelectorToken(SelectorDefaults.DateToday, Piped = PipedForm.Rejected)]
internal record DateTodaySelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], ResultTypes.Fixed(typeof(DateTime)), _ => target => target.CreateDateTimeStaticAccess("Today")),
	];

	public DateTodaySelector() : base(SelectorDefaults.DateToday) { }
	public DateTodaySelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateToday, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
