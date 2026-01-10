using System.ComponentModel.DataAnnotations;

namespace ExpDataWorkerBeta.Feature.PropertySelectors
{
	public enum FunctionTypes
	{
		[Display(Name = FunctionDefaults.CountAggregateFunction)]
		Count = 1,
		[Display(Name = FunctionDefaults.CountAggregateFunction)]
		CountDistinct,
		[Display(Name = FunctionDefaults.SumAggregateFunction)]
		Sum,
		[Display(Name = FunctionDefaults.AvgAggregateFunction)]
		Average,
		[Display(Name = FunctionDefaults.MinAggregateFunction)]
		Min,
		[Display(Name = FunctionDefaults.MaxAggregateFunction)]
		Max
	}
}
