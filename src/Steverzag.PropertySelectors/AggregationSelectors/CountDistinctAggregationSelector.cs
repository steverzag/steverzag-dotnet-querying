using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.AggregationSelectors;

[SelectorToken(SelectorDefaults.CountDistinctAggregateFunction, Category = FunctionCategory.Aggregation)]
public record CountDistinctAggregationSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public CountDistinctAggregationSelector() : base(SelectorDefaults.CountDistinctAggregateFunction) { }
	public static IReadOnlyList<FunctionSignature> Signatures => CountDistinctAggregationSelector.Table;
}

[SelectorToken(SelectorDefaults.CountDistinctAggregateFunction, Category = FunctionCategory.Aggregation)]
public record CountDistinctAggregationSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], ResultTypes.Fixed(typeof(int)),
			_ => target => target!.CreateAggregateCountDistinctFunction())
		{ Target = ArgumentConstraint.Collection },
	];

	public CountDistinctAggregationSelector() : base(SelectorDefaults.CountDistinctAggregateFunction) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
