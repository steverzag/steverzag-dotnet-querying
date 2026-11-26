using Steverzag.PropertySelectors.Registration;

namespace Steverzag.PropertySelectors.AggregationSelectors;

[SelectorToken(SelectorDefaults.CountAggregateFunction, Category = FunctionCategory.Aggregation)]
public record CountAggregationSelector<T> : FunctionPropertySelector<T>, IFunctionSignatures
{
	public CountAggregationSelector() : base(SelectorDefaults.CountAggregateFunction) { }
	public static IReadOnlyList<FunctionSignature> Signatures => CountAggregationSelector.Table;
}

[SelectorToken(SelectorDefaults.CountAggregateFunction, Category = FunctionCategory.Aggregation)]
public record CountAggregationSelector : FunctionPropertySelector, IFunctionSignatures
{
	internal static readonly IReadOnlyList<FunctionSignature> Table =
	[
		new([], ResultTypes.Fixed(typeof(int)),
			_ => target => target!.CreateAggregateCountFunction())
		{ Target = ArgumentConstraint.Collection },
	];

	public CountAggregationSelector() : base(SelectorDefaults.CountAggregateFunction) { }
	public static IReadOnlyList<FunctionSignature> Signatures => Table;
}
