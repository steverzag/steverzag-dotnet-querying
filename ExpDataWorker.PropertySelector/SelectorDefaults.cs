using System;
using System.Collections.Generic;
using System.Linq;

namespace ExpDataWorker.PropertySelectors
{
	public class SelectorDefaults
	{
		public const string PropertyDelimiter = "|";
		public const string ComplexSelectorStart = "$";
		public const string FunctionStart = ComplexSelectorStart + "FN_";
		public const string SpecifierStart = ComplexSelectorStart + "SPEC_";
		public const string ValueStart = ComplexSelectorStart + "VAL_";
		public const string ArithmeticStart = ComplexSelectorStart + "ARITH_";
		public const string ValuePrefix = ":";


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

		public const string MathCeiling = FunctionStart + "MATH_CEILING";
		public const string MathFloor = FunctionStart + "MATH_FLOOR";
		public const string MathRound = FunctionStart + "MATH_ROUND";
		public const string MathTruncate = FunctionStart + "MATH_TRUNCATE";
		public const string MathSign = FunctionStart + "MATH_SIGN";
		public const string MathPower = FunctionStart + "MATH_POW";
		public const string MathSqrt = FunctionStart + "MATH_SQRT";
		public const string MathExp = FunctionStart + "MATH_EXP";
		public const string MathLog = FunctionStart + "MATH_LOG";
		public const string MathLog10 = FunctionStart + "MATH_LOG10";
		public const string MathSin = FunctionStart + "MATH_SIN";
		public const string MathCos = FunctionStart + "MATH_COS";
		public const string MathTan = FunctionStart + "MATH_TAN";
		public const string MathAsin = FunctionStart + "MATH_ASIN";
		public const string MathAcos = FunctionStart + "MATH_ACOS";
		public const string MathAtan = FunctionStart + "MATH_ATAN";
		public const string MathAtan2 = FunctionStart + "MATH_ATAN2";
		public const string MathMax = FunctionStart + "MATH_MAX";
		public const string MathMin = FunctionStart + "MATH_MIN";

		public const string IntegerValueSpecifier = ValueStart + "INT";
		public const string DoubleValueSpecifier = ValueStart + "DOUBLE";
		public const string BooleanValueSpecifier = ValueStart + "BOOL";

		public const string ArithmeticOperation = ArithmeticStart + "OPERATION";
		public const string ArithmeticOperationEncloser = "::";
	}
}
