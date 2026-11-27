using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.AggregationSelectors;

[SelectorToken(SelectorDefaults.MaxAggregateFunction, Category = FunctionCategory.Aggregation)]
internal record MaxAggregationSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public MaxAggregationSelector() : base(SelectorDefaults.MaxAggregateFunction) { }
	public static IReadOnlyList<FunctionSignature> Signatures => MaxAggregationSelector.Table;
}

[SelectorToken(SelectorDefaults.MaxAggregateFunction, Category = FunctionCategory.Aggregation)]
internal record MaxAggregationSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], (t, _) => Implementations.LiftedAggregateType(nameof(Enumerable.Max), ResultTypes.ElementOf(t)),
			_ => target => target!.CreateAggregateMaxFunction())
		{ Target = ArgumentConstraint.Collection },
	];

	public MaxAggregationSelector() : base(SelectorDefaults.MaxAggregateFunction) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
