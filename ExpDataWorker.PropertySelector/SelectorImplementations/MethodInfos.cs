using System.Reflection;

namespace ExpDataWorker.PropertySelectors.SelectorImplementations
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
		internal readonly static MethodInfo MaxMethodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "Max" && m.GetParameters().Length == 1);
		internal readonly static MethodInfo MinMethodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "Min" && m.GetParameters().Length == 1);
		internal readonly static MethodInfo AverageMethodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "Average" && m.GetParameters().Length == 1);
		internal readonly static MethodInfo SumMethodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "Sum" && m.GetParameters().Length == 1);
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
		internal readonly static MethodInfo MathMaxMethodInfo = typeof(Math).GetMethods()
				.First(m => m.Name == "Max" && m.GetParameters()[0].ParameterType == typeof(double));
		internal readonly static MethodInfo MathMinMethodInfo = typeof(Math).GetMethods()
				.First(m => m.Name == "Min" && m.GetParameters()[0].ParameterType == typeof(double));
	}
}
