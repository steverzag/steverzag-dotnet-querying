using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.DateSelectors;

[SelectorToken(SelectorDefaults.DateNow, Piped = PipedForm.Rejected, Category = FunctionCategory.Date)]
internal record DateNowSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public DateNowSelector() : base(SelectorDefaults.DateNow) { }
	public DateNowSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateNow, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => DateNowSelector.Table;
}

[SelectorToken(SelectorDefaults.DateNow, Piped = PipedForm.Rejected, Category = FunctionCategory.Date)]
internal record DateNowSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], ResultTypes.Fixed(typeof(DateTime)), _ => target => target.CreateDateTimeStaticAccess("Now")),
	];

	public DateNowSelector() : base(SelectorDefaults.DateNow) { }
	public DateNowSelector(List<PropertySelector> parameters) : base(SelectorDefaults.DateNow, parameters) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
