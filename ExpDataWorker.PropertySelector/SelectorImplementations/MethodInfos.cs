using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelector.SelectorImplementations
{
	internal class MethodInfos
	{
		internal readonly static MethodInfo FirstOrDefaultMethodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "FirstOrDefault" && m.GetParameters().Length == 1);
		internal readonly static MethodInfo LastOrDefaultMethodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "LastOrDefault" && m.GetParameters().Length == 1);
		internal readonly static MethodInfo OrderByAscendingMehodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "OrderBy" && m.GetParameters().Length == 2);
		internal readonly static MethodInfo OrderByDescendingMehodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "OrderByDescending" && m.GetParameters().Length == 2);
		internal readonly static MethodInfo ThenOrderByAscendingMehodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "ThenBy" && m.GetParameters().Length == 2);
		internal readonly static MethodInfo ThenOrderByDescendingMehodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "ThenByDescending" && m.GetParameters().Length == 2);
		internal readonly static MethodInfo CountMethodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "Count" && m.GetParameters().Length == 1);
		internal readonly static MethodInfo MaxMethodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "Max" && m.GetParameters().Length == 1);
		internal readonly static MethodInfo MinMethodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "Min" && m.GetParameters().Length == 1);
		internal readonly static MethodInfo AverageMethodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "Average" && m.GetParameters().Length == 1);
		internal readonly static MethodInfo SumMethodInfo = typeof(Enumerable).GetMethods()
				.First(m => m.Name == "Sum" && m.GetParameters().Length == 1);

	}
}
