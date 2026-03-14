using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelectors
{
	public class SelectorDefaults
	{
		public const string FunctionStart = "*FN_";
		public const string SpecifierStart = "*SPEC_";

		public const string CountAggregateFunction = FunctionStart + "AGGR_COUNT";
		public const string LongCountAggregateFunction = FunctionStart + "AGGR_LONG_COUNT";
		public const string CountDistinctAggregateFunction = FunctionStart + "AGGR_COUNTDISTINCT";
		public const string SumAggregateFunction = FunctionStart + "AGGR_SUM";
		public const string AvgAggregateFunction = FunctionStart + "AGGR_AVG";
		public const string MinAggregateFunction = FunctionStart + "AGGR_MIN";
		public const string MaxAggregateFunction = FunctionStart + "AGGR_MAX";

		public const string ElementFirst = FunctionStart + "ELM_FIRST";
		public const string ElementLast = FunctionStart + "ELM_LAST";
		public const string ElementAtIndex = FunctionStart + "ELM_ATINDEX";

		public const string OrderAscending = FunctionStart + "ORDER_ASC";
		public const string OrderDescending = FunctionStart + "ORDER_DESC";
		public const string ThenOrderByAscending = FunctionStart + "THEN_ORDER_ASC";
		public const string ThenOrderByDescending = FunctionStart + "THEN_ORDER_DESC";

		public const string IntegerValueSpecifier = SpecifierStart + "INT_VALUE";
		public const string DoubleValueSpecifier = SpecifierStart + "DOUBLE_VALUE";
		public const string BooleanValueSpecifier = SpecifierStart + "BOOL_VALUE";
	}
}
