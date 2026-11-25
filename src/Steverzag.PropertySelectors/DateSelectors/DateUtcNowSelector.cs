using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateUtcNow, Piped = PipedForm.Rejected)]
internal record DateUtcNowSelector<T> : FunctionPropertySelector<T>
{
	public DateUtcNowSelector() : base(SelectorDefaults.DateUtcNow) { }
	public DateUtcNowSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateUtcNow, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => DateUtcNowSelector.Table;
}

[SelectorToken(SelectorDefaults.DateUtcNow, Piped = PipedForm.Rejected)]
internal record DateUtcNowSelector : FunctionPropertySelector
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], ResultTypes.Fixed(typeof(DateTime)), _ => target => target.CreateDateTimeStaticAccess("UtcNow")),
	];

	public DateUtcNowSelector() : base(SelectorDefaults.DateUtcNow) { }
	public DateUtcNowSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateUtcNow, parameters) { }
	protected override IReadOnlyList<FunctionSignature> Signatures => Table;
}
