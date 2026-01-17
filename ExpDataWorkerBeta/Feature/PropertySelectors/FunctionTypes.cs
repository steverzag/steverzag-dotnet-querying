using System.ComponentModel.DataAnnotations;

namespace ExpDataWorkerBeta.Feature.PropertySelectors
{
	public enum FunctionTypes
	{
		[Display(Name = SelectorDefaults.CountAggregateFunction)]
		Count = 1,
		[Display(Name = SelectorDefaults.CountAggregateFunction)]
		CountDistinct,
		[Display(Name = SelectorDefaults.SumAggregateFunction)]
		Sum,
		[Display(Name = SelectorDefaults.AvgAggregateFunction)]
		Average,
		[Display(Name = SelectorDefaults.MinAggregateFunction)]
		Min,
		[Display(Name = SelectorDefaults.MaxAggregateFunction)]
		Max
	}
}
