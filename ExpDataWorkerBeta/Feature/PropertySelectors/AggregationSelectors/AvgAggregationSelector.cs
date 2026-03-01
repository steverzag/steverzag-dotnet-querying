
using ExpDataWorkerBeta.Feature.PropertySelectors.ElementSelectors;

namespace ExpDataWorkerBeta.Feature.PropertySelectors.AggregationSelectors
{
	public record AvgAggregationSelector : FunctionPropertySelector
	{
		public AvgAggregationSelector() : base(SelectorDefaults.AvgAggregateFunction)
		{
			//CreateAccessor = (target) => target.CreateAggregateAvgFunction();
		}

		public override void InitializeOverloads()
		{
			// Overload with no parameters
			var emptyFunctionParameters = new FunctionPropertySelectorOverload([], (target) => target.CreateAggregateAvgFunction());
			Overloads.Add(emptyFunctionParameters);
		}
	}
}
