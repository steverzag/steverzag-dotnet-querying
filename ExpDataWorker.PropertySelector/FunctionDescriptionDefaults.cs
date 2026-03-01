using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelector
{
	public class SelectorDefaults
	{
		public const string FunctionStart = "*FN_";
		public const string SpecifierStart = "*SPEC_";

		public const string CountAggregateFunction = FunctionStart + "AGGR_COUNT";
		public const string CountDistinctAggregateFunction = FunctionStart + "AGGR_COUNTDISTINCT";
		public const string SumAggregateFunction = FunctionStart + "AGGR_SUM";
		public const string AvgAggregateFunction = FunctionStart + "AGGR_AVG";
		public const string MinAggregateFunction = FunctionStart + "AGGR_MIN";
		public const string MaxAggregateFunction = FunctionStart + "AGGR_MAX";

		public const string ElementFirst = FunctionStart + "ELM_FIRST";
		public const string ElementLast = FunctionStart + "ELM_LAST";
		public const string ElementAtIndex = FunctionStart + "ELM_ATINDEX";

		public const string OrderAscending = SpecifierStart + "ORDER_ASC";
		public const string OrderDescending = SpecifierStart + "ORDER_DESC";
		public const string ThenOrderByAscending = SpecifierStart + "THEN_ORDER_ASC";
		public const string ThenOrderByDescending = SpecifierStart + "THEN_ORDER_DESC";
	}
}
