using System.Reflection;

namespace Steverzag.PropertySelectors.SelectorImplementations
{
	internal class MethodInfos
	{
		internal readonly static MethodInfo FirstOrDefaultMethodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "FirstOrDefault" && m.GetParameters().Length == 1);
		internal readonly static MethodInfo SelectMethodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "Select" && m.GetParameters().Length == 2);
		internal readonly static MethodInfo SelectManyMethodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "SelectMany" && m.GetParameters().Length == 2);
		internal readonly static MethodInfo LastOrDefaultMethodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "LastOrDefault" && m.GetParameters().Length == 1);
		internal readonly static MethodInfo SkipMethodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "Skip" && m.GetParameters().Length == 2);
		internal readonly static MethodInfo OrderByAscendingMethodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "OrderBy" && m.GetParameters().Length == 2);
		internal readonly static MethodInfo OrderByDescendingMethodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "OrderByDescending" && m.GetParameters().Length == 2);
		internal readonly static MethodInfo ThenOrderByAscendingMethodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "ThenBy" && m.GetParameters().Length == 2);
		internal readonly static MethodInfo ThenOrderByDescendingMethodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "ThenByDescending" && m.GetParameters().Length == 2);
		internal readonly static MethodInfo CountMethodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "Count" && m.GetParameters().Length == 1);
		internal readonly static MethodInfo LongCountMethodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "LongCount" && m.GetParameters().Length == 1);
		internal readonly static MethodInfo WhereMethodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "Where" && m.GetParameters().Length == 2
					&& m.GetParameters()[1].ParameterType.GetGenericArguments().Length == 2);
		internal readonly static MethodInfo DistinctMethodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "Distinct" && m.GetParameters().Length == 1);
		internal readonly static MethodInfo MathPowerMethodInfo = typeof(Math).GetMethods()
				.First(m => m.Name == "Pow" && m.GetParameters().Length == 2);
		internal readonly static MethodInfo MathCeilingMethodInfo = typeof(Math).GetMethods()
				.First(m => m.Name == "Ceiling" && m.GetParameters().Length == 1);
		internal readonly static MethodInfo MathFloorMethodInfo = typeof(Math).GetMethods()
				.First(m => m.Name == "Floor" && m.GetParameters().Length == 1);
		internal readonly static MethodInfo MathRoundMethodInfo = typeof(Math).GetMethods()
				.First(m => m.Name == "Round" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(double));
		internal readonly static MethodInfo MathRound_2_MethodInfo = typeof(Math).GetMethods()
				.First(m => m.Name == "Round" && m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType == typeof(double));
		internal readonly static MethodInfo MathTruncateMethodInfo = typeof(Math).GetMethods()
				.First(m => m.Name == "Truncate" && m.GetParameters()[0].ParameterType == typeof(double));
		internal readonly static MethodInfo MathSignMethodInfo = typeof(Math).GetMethods()
				.First(m => m.Name == "Sign" && m.GetParameters()[0].ParameterType == typeof(double));
		internal readonly static MethodInfo MathSqrtMethodInfo = typeof(Math).GetMethods()
				.First(m => m.Name == "Sqrt" && m.GetParameters().Length == 1);
		internal readonly static MethodInfo MathExpMethodInfo = typeof(Math).GetMethods()
				.First(m => m.Name == "Exp" && m.GetParameters().Length == 1);
		internal readonly static MethodInfo MathLogMethodInfo = typeof(Math).GetMethods()
				.First(m => m.Name == "Log" && m.GetParameters().Length == 1);
		internal readonly static MethodInfo MathLog10MethodInfo = typeof(Math).GetMethods()
				.First(m => m.Name == "Log10" && m.GetParameters().Length == 1);
		internal readonly static MethodInfo SinMethodInfo = typeof(Math).GetMethods()
				.First(m => m.Name == "Sin" && m.GetParameters().Length == 1);
		internal readonly static MethodInfo MathCosMethodInfo = typeof(Math).GetMethods()
				.First(m => m.Name == "Cos" && m.GetParameters().Length == 1);
		internal readonly static MethodInfo MathTanMethodInfo = typeof(Math).GetMethods()
				.First(m => m.Name == "Tan" && m.GetParameters().Length == 1);
		internal readonly static MethodInfo MathAsinMethodInfo = typeof(Math).GetMethods()
				.First(m => m.Name == "Asin" && m.GetParameters().Length == 1);
		internal readonly static MethodInfo MathAcosMethodInfo = typeof(Math).GetMethods()
				.First(m => m.Name == "Acos" && m.GetParameters().Length == 1);
		internal readonly static MethodInfo MathAtanMethodInfo = typeof(Math).GetMethods()
				.First(m => m.Name == "Atan" && m.GetParameters().Length == 1);
		internal readonly static MethodInfo MathAtan2MethodInfo = typeof(Math).GetMethods()
				.First(m => m.Name == "Atan2" && m.GetParameters().Length == 2);
		internal readonly static MethodInfo StringToUpperMethodInfo = typeof(string).GetMethods()
				.First(m => m.Name == "ToUpper" && m.GetParameters().Length == 0);
		internal readonly static MethodInfo StringToLowerMethodInfo = typeof(string).GetMethods()
				.First(m => m.Name == "ToLower" && m.GetParameters().Length == 0);
		internal readonly static MethodInfo StringTrimMethodInfo = typeof(string).GetMethods()
				.First(m => m.Name == "Trim" && m.GetParameters().Length == 0);
		internal readonly static MethodInfo StringTrimStartMethodInfo = typeof(string).GetMethods()
				.First(m => m.Name == "TrimStart" && m.GetParameters().Length == 0);
		internal readonly static MethodInfo StringTrimEndMethodInfo = typeof(string).GetMethods()
				.First(m => m.Name == "TrimEnd" && m.GetParameters().Length == 0);
		internal readonly static MethodInfo StringSubstringMethodInfo = typeof(string).GetMethods()
				.First(m => m.Name == "Substring" && m.GetParameters().Length == 1);
		internal readonly static MethodInfo StringSubstring_2_MethodInfo = typeof(string).GetMethods()
				.First(m => m.Name == "Substring" && m.GetParameters().Length == 2);
		internal readonly static MethodInfo StringIndexOfMethodInfo = typeof(string).GetMethods()
				.First(m => m.Name == "IndexOf" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string));
		internal readonly static MethodInfo StringReplaceMethodInfo = typeof(string).GetMethods()
				.First(m => m.Name == "Replace" && m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType == typeof(string));
		internal readonly static MethodInfo StringContainsMethodInfo = typeof(string).GetMethods()
				.First(m => m.Name == "Contains" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string));
		internal readonly static MethodInfo StringStartsWithMethodInfo = typeof(string).GetMethods()
				.First(m => m.Name == "StartsWith" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string));
		internal readonly static MethodInfo StringEndsWithMethodInfo = typeof(string).GetMethods()
				.First(m => m.Name == "EndsWith" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string));
		internal readonly static MethodInfo StringConcat_2_MethodInfo = typeof(string).GetMethods()
				.First(m => m.Name == "Concat" && m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType == typeof(string));
		internal readonly static MethodInfo StringConcat_3_MethodInfo = typeof(string).GetMethods()
				.First(m => m.Name == "Concat" && m.GetParameters().Length == 3 && m.GetParameters()[0].ParameterType == typeof(string));
		internal readonly static MethodInfo StringConcat_4_MethodInfo = typeof(string).GetMethods()
				.First(m => m.Name == "Concat" && m.GetParameters().Length == 4 && m.GetParameters()[0].ParameterType == typeof(string));
		// string.Join(string separator, params string[] value): the array overload is the shape
		// relational providers translate (CONCAT_WS on SQL Server, EF Core 7 and later).
		internal readonly static MethodInfo StringJoinMethodInfo = typeof(string).GetMethods()
				.First(m => m.Name == "Join" && m.GetParameters().Length == 2
					&& m.GetParameters()[0].ParameterType == typeof(string)
					&& m.GetParameters()[1].ParameterType == typeof(string[]));
		internal readonly static MethodInfo StringCompareMethodInfo = typeof(string).GetMethods()
				.First(m => m.Name == "Compare" && m.GetParameters().Length == 2
					&& m.GetParameters()[0].ParameterType == typeof(string)
					&& m.GetParameters()[1].ParameterType == typeof(string));
		internal readonly static MethodInfo StringIsNullOrEmptyMethodInfo = typeof(string).GetMethods()
				.First(m => m.Name == "IsNullOrEmpty" && m.GetParameters().Length == 1);
		internal readonly static MethodInfo StringIsNullOrWhiteSpaceMethodInfo = typeof(string).GetMethods()
				.First(m => m.Name == "IsNullOrWhiteSpace" && m.GetParameters().Length == 1);
	}
}
