using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateUtcNow, Piped = PipedForm.Rejected, Category = FunctionCategory.Date)]
internal record DateUtcNowSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public DateUtcNowSelector() : base(SelectorDefaults.DateUtcNow) { }
	public DateUtcNowSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateUtcNow, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => DateUtcNowSelector.Table;
}

[SelectorToken(SelectorDefaults.DateUtcNow, Piped = PipedForm.Rejected, Category = FunctionCategory.Date)]
internal record DateUtcNowSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], ResultTypes.Fixed(typeof(DateTime)), _ => target => target.CreateDateTimeStaticAccess("UtcNow")),
	];

	public DateUtcNowSelector() : base(SelectorDefaults.DateUtcNow) { }
	public DateUtcNowSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateUtcNow, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
