using System;
using System.Collections.Generic;
using System.Linq;

namespace Steverzag.PropertySelectors
{
	/// <summary>
	/// The selector grammar's special characters and the token of every built-in function, for building selector text in
	/// code.
	/// </summary>
	/// <remarks>
	/// The tokens are frozen with the grammar, so the values won't change within a major version. What each function
	/// does is described in "Function catalog" in <c>docs/property-selectors.md</c>, and
	/// <see cref="MetaData.FunctionCatalog"/> lists them at runtime with their signatures.
	/// </remarks>
	public static class SelectorDefaults
	{
		/// <summary>The pipe, <c>|</c>: passes the value on its left to the part on its right.</summary>
		public const string PropertyDelimiter = "|";
		/// <summary>The character that starts every function token, <c>$</c>. A part that starts with it is never a property.</summary>
		public const string ComplexSelectorStart = "$";
		/// <summary>The prefix of every function token, <c>$FN_</c>.</summary>
		public const string FunctionStart = ComplexSelectorStart + "FN_";
		internal const string ArithmeticStart = ComplexSelectorStart + "ARITH_";

		/// <summary><c>$FN_AGGR_COUNT</c>: the number of elements in a collection, as an <see cref="int"/>.</summary>
		public const string CountAggregateFunction = FunctionStart + "AGGR_COUNT";
		/// <summary><c>$FN_AGGR_LONG_COUNT</c>: the number of elements in a collection, as a <see cref="long"/>.</summary>
		public const string LongCountAggregateFunction = FunctionStart + "AGGR_LONG_COUNT";
		/// <summary><c>$FN_AGGR_COUNTDISTINCT</c>: the number of distinct non-null values in a collection, like SQL's <c>COUNT(DISTINCT x)</c>.</summary>
		public const string CountDistinctAggregateFunction = FunctionStart + "AGGR_COUNTDISTINCT";
		/// <summary><c>$FN_AGGR_SUM</c>: the sum of a collection of numbers. An empty collection sums to 0.</summary>
		public const string SumAggregateFunction = FunctionStart + "AGGR_SUM";
		/// <summary><c>$FN_AGGR_AVG</c>: the average of a collection of numbers, or <see langword="null"/> for an empty collection.</summary>
		public const string AvgAggregateFunction = FunctionStart + "AGGR_AVG";
		/// <summary><c>$FN_AGGR_MIN</c>: the smallest value in a collection, or <see langword="null"/> for an empty collection.</summary>
		public const string MinAggregateFunction = FunctionStart + "AGGR_MIN";
		/// <summary><c>$FN_AGGR_MAX</c>: the largest value in a collection, or <see langword="null"/> for an empty collection.</summary>
		public const string MaxAggregateFunction = FunctionStart + "AGGR_MAX";

		/// <summary><c>$FN_ELM_FIRST</c>: the first element of a collection, or the default value if it's empty.</summary>
		public const string ElementFirst = FunctionStart + "ELM_FIRST";
		/// <summary><c>$FN_ELM_LAST</c>: the last element of a collection, or the default value if it's empty.</summary>
		public const string ElementLast = FunctionStart + "ELM_LAST";
		/// <summary><c>$FN_ELM_ATINDEX</c>: the element at an index given as an integer literal, or the default value if there is none.</summary>
		public const string ElementAtIndex = FunctionStart + "ELM_ATINDEX";

		/// <summary><c>$FN_ORDER_ASC</c>: orders a collection by a key, ascending.</summary>
		public const string OrderAscending = FunctionStart + "ORDER_ASC";
		/// <summary><c>$FN_ORDER_DESC</c>: orders a collection by a key, descending.</summary>
		public const string OrderDescending = FunctionStart + "ORDER_DESC";
		/// <summary><c>$FN_THEN_ORDER_ASC</c>: adds an ascending key to an ordering. It must follow <c>ORDER_ASC</c> or <c>ORDER_DESC</c>.</summary>
		public const string ThenOrderByAscending = FunctionStart + "THEN_ORDER_ASC";
		/// <summary><c>$FN_THEN_ORDER_DESC</c>: adds a descending key to an ordering. It must follow <c>ORDER_ASC</c> or <c>ORDER_DESC</c>.</summary>
		public const string ThenOrderByDescending = FunctionStart + "THEN_ORDER_DESC";

		/// <summary><c>$FN_MATH_CEILING</c>: rounds a number up. Exact on a <see cref="decimal"/>.</summary>
		public const string MathCeiling = FunctionStart + "MATH_CEILING";
		/// <summary><c>$FN_MATH_FLOOR</c>: rounds a number down. Exact on a <see cref="decimal"/>.</summary>
		public const string MathFloor = FunctionStart + "MATH_FLOOR";
		/// <summary><c>$FN_MATH_ROUND</c>: rounds a number, optionally to a number of decimal places. Exact on a <see cref="decimal"/>.</summary>
		public const string MathRound = FunctionStart + "MATH_ROUND";
		/// <summary><c>$FN_MATH_TRUNCATE</c>: drops a number's fractional part. Exact on a <see cref="decimal"/>.</summary>
		public const string MathTruncate = FunctionStart + "MATH_TRUNCATE";
		/// <summary><c>$FN_MATH_SIGN</c>: -1, 0 or 1, by the sign of a number.</summary>
		public const string MathSign = FunctionStart + "MATH_SIGN";
		/// <summary><c>$FN_MATH_POW</c>: a number raised to a power.</summary>
		public const string MathPower = FunctionStart + "MATH_POW";
		/// <summary><c>$FN_MATH_SQRT</c>: the square root of a number.</summary>
		public const string MathSqrt = FunctionStart + "MATH_SQRT";
		/// <summary><c>$FN_MATH_EXP</c>: e raised to a number.</summary>
		public const string MathExp = FunctionStart + "MATH_EXP";
		/// <summary><c>$FN_MATH_LOG</c>: the natural logarithm of a number, or its logarithm in a given base.</summary>
		public const string MathLog = FunctionStart + "MATH_LOG";
		/// <summary><c>$FN_MATH_LOG10</c>: the base-10 logarithm of a number.</summary>
		public const string MathLog10 = FunctionStart + "MATH_LOG10";
		/// <summary><c>$FN_MATH_SIN</c>: the sine of an angle in radians.</summary>
		public const string MathSin = FunctionStart + "MATH_SIN";
		/// <summary><c>$FN_MATH_COS</c>: the cosine of an angle in radians.</summary>
		public const string MathCos = FunctionStart + "MATH_COS";
		/// <summary><c>$FN_MATH_TAN</c>: the tangent of an angle in radians.</summary>
		public const string MathTan = FunctionStart + "MATH_TAN";
		/// <summary><c>$FN_MATH_ASIN</c>: the angle, in radians, whose sine is a number.</summary>
		public const string MathAsin = FunctionStart + "MATH_ASIN";
		/// <summary><c>$FN_MATH_ACOS</c>: the angle, in radians, whose cosine is a number.</summary>
		public const string MathAcos = FunctionStart + "MATH_ACOS";
		/// <summary><c>$FN_MATH_ATAN</c>: the angle, in radians, whose tangent is a number.</summary>
		public const string MathAtan = FunctionStart + "MATH_ATAN";
		/// <summary><c>$FN_MATH_ATAN2</c>: the angle, in radians, of the point (x, y), given y and x.</summary>
		public const string MathAtan2 = FunctionStart + "MATH_ATAN2";
		/// <summary>
		/// <c>$FN_MATH_MAX</c>: the larger of two values in the same row, ignoring a null. For the largest value in a
		/// collection, use <see cref="MaxAggregateFunction"/>.
		/// </summary>
		public const string MathMax = FunctionStart + "MATH_MAX";
		/// <summary>
		/// <c>$FN_MATH_MIN</c>: the smaller of two values in the same row, ignoring a null. For the smallest value in a
		/// collection, use <see cref="MinAggregateFunction"/>.
		/// </summary>
		public const string MathMin = FunctionStart + "MATH_MIN";

		/// <summary><c>$FN_STR_LENGTH</c>: the number of characters in a string.</summary>
		public const string StringLength = FunctionStart + "STR_LENGTH";
		/// <summary><c>$FN_STR_TOUPPER</c>: a string in upper case.</summary>
		public const string StringToUpper = FunctionStart + "STR_TOUPPER";
		/// <summary><c>$FN_STR_TOLOWER</c>: a string in lower case.</summary>
		public const string StringToLower = FunctionStart + "STR_TOLOWER";
		/// <summary><c>$FN_STR_TRIM</c>: a string without leading and trailing white space.</summary>
		public const string StringTrim = FunctionStart + "STR_TRIM";
		/// <summary><c>$FN_STR_TRIMSTART</c>: a string without leading white space.</summary>
		public const string StringTrimStart = FunctionStart + "STR_TRIMSTART";
		/// <summary><c>$FN_STR_TRIMEND</c>: a string without trailing white space.</summary>
		public const string StringTrimEnd = FunctionStart + "STR_TRIMEND";
		/// <summary><c>$FN_STR_SUBSTRING</c>: part of a string, from a start index and optionally for a length.</summary>
		public const string StringSubstring = FunctionStart + "STR_SUBSTRING";
		/// <summary><c>$FN_STR_INDEXOF</c>: the index of the first occurrence of a string, or -1.</summary>
		public const string StringIndexOf = FunctionStart + "STR_INDEXOF";
		/// <summary><c>$FN_STR_REPLACE</c>: a string with every occurrence of one string replaced by another.</summary>
		public const string StringReplace = FunctionStart + "STR_REPLACE";
		/// <summary><c>$FN_STR_CONTAINS</c>: whether a string contains another.</summary>
		public const string StringContains = FunctionStart + "STR_CONTAINS";
		/// <summary><c>$FN_STR_STARTSWITH</c>: whether a string starts with another.</summary>
		public const string StringStartsWith = FunctionStart + "STR_STARTSWITH";
		/// <summary><c>$FN_STR_ENDSWITH</c>: whether a string ends with another.</summary>
		public const string StringEndsWith = FunctionStart + "STR_ENDSWITH";
		/// <summary><c>$FN_STR_CONCAT</c>: strings joined end to end.</summary>
		public const string StringConcat = FunctionStart + "STR_CONCAT";
		/// <summary><c>$FN_STR_JOIN</c>: values joined with a separator. It takes no piped value.</summary>
		public const string StringJoin = FunctionStart + "STR_JOIN";
		/// <summary><c>$FN_STR_COMPARE</c>: a negative number, 0 or a positive number, by how two strings sort.</summary>
		public const string StringCompare = FunctionStart + "STR_COMPARE";
		/// <summary><c>$FN_STR_ISNULLOREMPTY</c>: whether a string is null or empty.</summary>
		public const string StringIsNullOrEmpty = FunctionStart + "STR_ISNULLOREMPTY";
		/// <summary><c>$FN_STR_ISNULLORWHITESPACE</c>: whether a string is null, empty or only white space.</summary>
		public const string StringIsNullOrWhiteSpace = FunctionStart + "STR_ISNULLORWHITESPACE";

		/// <summary><c>$FN_DT_ADDYEARS</c>: a date plus a number of years. Takes <see cref="DateTime"/>, <see cref="DateTimeOffset"/> and <see cref="DateOnly"/>.</summary>
		public const string DateAddYears = FunctionStart + "DT_ADDYEARS";
		/// <summary><c>$FN_DT_ADDMONTHS</c>: a date plus a number of months. Takes <see cref="DateTime"/>, <see cref="DateTimeOffset"/> and <see cref="DateOnly"/>.</summary>
		public const string DateAddMonths = FunctionStart + "DT_ADDMONTHS";
		/// <summary><c>$FN_DT_ADDDAYS</c>: a date plus a number of days. Takes <see cref="DateTime"/>, <see cref="DateTimeOffset"/> and <see cref="DateOnly"/>.</summary>
		public const string DateAddDays = FunctionStart + "DT_ADDDAYS";
		/// <summary><c>$FN_DT_ADDHOURS</c>: a date or time plus a number of hours. Takes <see cref="DateTime"/>, <see cref="DateTimeOffset"/> and <see cref="TimeOnly"/>.</summary>
		public const string DateAddHours = FunctionStart + "DT_ADDHOURS";
		/// <summary><c>$FN_DT_ADDMINUTES</c>: a date or time plus a number of minutes. Takes <see cref="DateTime"/>, <see cref="DateTimeOffset"/> and <see cref="TimeOnly"/>.</summary>
		public const string DateAddMinutes = FunctionStart + "DT_ADDMINUTES";
		/// <summary><c>$FN_DT_ADDSECONDS</c>: a date plus a number of seconds. Takes <see cref="DateTime"/> and <see cref="DateTimeOffset"/>.</summary>
		public const string DateAddSeconds = FunctionStart + "DT_ADDSECONDS";
		/// <summary><c>$FN_DT_ADDMILLISECONDS</c>: a date plus a number of milliseconds. Takes <see cref="DateTime"/> and <see cref="DateTimeOffset"/>.</summary>
		public const string DateAddMilliseconds = FunctionStart + "DT_ADDMILLISECONDS";
		/// <summary><c>$FN_DT_NOW</c>: the current local date and time. It takes no piped value.</summary>
		public const string DateNow = FunctionStart + "DT_NOW";
		/// <summary><c>$FN_DT_UTCNOW</c>: the current UTC date and time. It takes no piped value.</summary>
		public const string DateUtcNow = FunctionStart + "DT_UTCNOW";
		/// <summary><c>$FN_DT_TODAY</c>: the current local date, at midnight. It takes no piped value.</summary>
		public const string DateToday = FunctionStart + "DT_TODAY";
		/// <summary><c>$FN_DT_DATETIME</c>: a <see cref="DateTime"/> literal from a quoted string, read in the invariant culture, such as <c>"2024-01-31T08:30:00Z"</c>.</summary>
		public const string DateTimeLiteral = FunctionStart + "DT_DATETIME";
		/// <summary><c>$FN_DT_DATEONLY</c>: a <see cref="DateOnly"/> literal from a quoted string, read in the invariant culture, such as <c>"2024-01-31"</c>.</summary>
		public const string DateOnlyLiteral = FunctionStart + "DT_DATEONLY";
		/// <summary><c>$FN_DT_TIMEONLY</c>: a <see cref="TimeOnly"/> literal from a quoted string, read in the invariant culture, such as <c>"08:30"</c>.</summary>
		public const string TimeOnlyLiteral = FunctionStart + "DT_TIMEONLY";

		/// <summary><c>$FN_COALESCE</c>: the piped value, or else the first of its arguments that isn't null, like C#'s <c>??</c>.</summary>
		public const string Coalesce = FunctionStart + "COALESCE";

		internal const string ArithmeticOperation = ArithmeticStart + "OPERATION";
		/// <summary>The character <c>:</c>, reserved for future syntax. It can appear only inside a quoted string.</summary>
		public const char ReservedCharacter = ':';
		/// <summary>The property-name escape, <c>@</c>: <c>@True</c> names a property called <c>True</c> instead of the literal.</summary>
		public const char PropertyNameEscape = '@';
	}
}
