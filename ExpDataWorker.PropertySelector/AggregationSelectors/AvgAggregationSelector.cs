
namespace ExpDataWorker.PropertySelector.AggregationSelectors
{
	public record AvgAggregationSelector : FunctionPropertySelector
	{
		public AvgAggregationSelector() : base(SelectorDefaults.AvgAggregateFunction)
		{
		}

		public override void InitializeOverloads()
		{
			// Overload with no parameters => Avg()
			var emptyFunctionParameters = new FunctionPropertySelectorOverload([], (target) => target.CreateAggregateAvgFunction());
			Overloads.Add(emptyFunctionParameters);
		}
	}
}
