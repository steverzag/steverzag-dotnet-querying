using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateNow, Piped = PipedForm.Rejected)]
internal record DateNowSelector<T> : FunctionPropertySelector<T>
{
	public DateNowSelector() : base(SelectorDefaults.DateNow) { }
	public DateNowSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateNow, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => DateNowSelector.Table;
}

[SelectorToken(SelectorDefaults.DateNow, Piped = PipedForm.Rejected)]
internal record DateNowSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], ResultTypes.Fixed(typeof(DateTime)), _ => target => target.CreateDateTimeStaticAccess("Now")),
	];

	public DateNowSelector() : base(SelectorDefaults.DateNow) { }
	public DateNowSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateNow, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
