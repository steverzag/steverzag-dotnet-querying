using System;
using System.Collections.Generic;
using System.Linq;

namespace Steverzag.PropertySelectors
{
	public class SelectorDefaults
	{
		public const string PropertyDelimiter = "|";
		public const string ComplexSelectorStart = "$";
		public const string FunctionStart = ComplexSelectorStart + "FN_";
		internal const string ArithmeticStart = ComplexSelectorStart + "ARITH_";


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

		public const string StringLength = FunctionStart + "STR_LENGTH";
		public const string StringToUpper = FunctionStart + "STR_TOUPPER";
		public const string StringToLower = FunctionStart + "STR_TOLOWER";
		public const string StringTrim = FunctionStart + "STR_TRIM";
		public const string StringTrimStart = FunctionStart + "STR_TRIMSTART";
		public const string StringTrimEnd = FunctionStart + "STR_TRIMEND";
		public const string StringSubstring = FunctionStart + "STR_SUBSTRING";
		public const string StringIndexOf = FunctionStart + "STR_INDEXOF";
		public const string StringReplace = FunctionStart + "STR_REPLACE";
		public const string StringContains = FunctionStart + "STR_CONTAINS";
		public const string StringStartsWith = FunctionStart + "STR_STARTSWITH";
		public const string StringEndsWith = FunctionStart + "STR_ENDSWITH";
		public const string StringConcat = FunctionStart + "STR_CONCAT";
		public const string StringJoin = FunctionStart + "STR_JOIN";
		public const string StringCompare = FunctionStart + "STR_COMPARE";
		public const string StringIsNullOrEmpty = FunctionStart + "STR_ISNULLOREMPTY";
		public const string StringIsNullOrWhiteSpace = FunctionStart + "STR_ISNULLORWHITESPACE";

		public const string DateAddYears = FunctionStart + "DT_ADDYEARS";
		public const string DateAddMonths = FunctionStart + "DT_ADDMONTHS";
		public const string DateAddDays = FunctionStart + "DT_ADDDAYS";
		public const string DateAddHours = FunctionStart + "DT_ADDHOURS";
		public const string DateAddMinutes = FunctionStart + "DT_ADDMINUTES";
		public const string DateAddSeconds = FunctionStart + "DT_ADDSECONDS";
		public const string DateAddMilliseconds = FunctionStart + "DT_ADDMILLISECONDS";
		public const string DateNow = FunctionStart + "DT_NOW";
		public const string DateUtcNow = FunctionStart + "DT_UTCNOW";
		public const string DateToday = FunctionStart + "DT_TODAY";
		public const string DateTimeLiteral = FunctionStart + "DT_DATETIME";
		public const string DateOnlyLiteral = FunctionStart + "DT_DATEONLY";
		public const string TimeOnlyLiteral = FunctionStart + "DT_TIMEONLY";

		public const string Coalesce = FunctionStart + "COALESCE";


		internal const string ArithmeticOperation = ArithmeticStart + "OPERATION";
		public const char ReservedCharacter = ':';
		public const char PropertyNameEscape = '@';
	}
}
