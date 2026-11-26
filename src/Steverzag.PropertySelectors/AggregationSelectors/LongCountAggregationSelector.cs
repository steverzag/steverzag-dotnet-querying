using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.AggregationSelectors;

[SelectorToken(SelectorDefaults.LongCountAggregateFunction, Category = FunctionCategory.Aggregation)]
public record LongCountAggregationSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public LongCountAggregationSelector() : base(SelectorDefaults.LongCountAggregateFunction) { }
	public static IReadOnlyList<FunctionSignature> Signatures => LongCountAggregationSelector.Table;
}

[SelectorToken(SelectorDefaults.LongCountAggregateFunction, Category = FunctionCategory.Aggregation)]
public record LongCountAggregationSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], ResultTypes.Fixed(typeof(long)),
			_ => target => target!.CreateAggregateLongCountFunction())
		{ Target = ArgumentConstraint.Collection },
	];

	public LongCountAggregationSelector() : base(SelectorDefaults.LongCountAggregateFunction) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
