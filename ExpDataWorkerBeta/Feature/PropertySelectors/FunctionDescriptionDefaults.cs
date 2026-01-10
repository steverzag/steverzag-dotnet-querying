using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorkerBeta.Feature.PropertySelectors
{
	public class FunctionDefaults
	{
		public const string FunctionStart = "*FN_";
		public const string CountAggregateFunction = FunctionStart + "AGGR_COUNT";
		public const string CountDistinctAggregateFunction = FunctionStart + "AGGR_COUNTDISTINCT";
		public const string SumAggregateFunction = FunctionStart + "AGGR_SUM";
		public const string AvgAggregateFunction = FunctionStart + "AGGR_AVG";
		public const string MinAggregateFunction = FunctionStart + "AGGR_MIN";
		public const string MaxAggregateFunction = FunctionStart + "AGGR_MAX";

	}
}
