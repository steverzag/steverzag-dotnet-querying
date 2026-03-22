using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelectors.Extensions
{
	internal static class PropertySelectorExtensions
	{

		internal static List<string> SplitPropertySelectors(this string expression)
		{
			var propertySelectors = new List<string>();
			var parenthesesCount = 0;
			var propertySelectorStartIndex = 0;
			for (int i = 0; i < expression.Length; i++)
			{
				if (i == expression.Length - 1)
				{
					var propertySelector = expression[propertySelectorStartIndex..].Trim();
					propertySelectors.Add(propertySelector);
				}
				if (expression[i] == '(')
				{
					parenthesesCount++;
					continue;
				}
				else if (expression[i] == ')')
				{
					parenthesesCount--;
					continue;
				}
				else if (parenthesesCount > 0)
					continue;
				else if (expression[i] == SelectorDefaults.PropertyDelimiter[0])
				{
					var propertySelector = expression[propertySelectorStartIndex..i].Trim();
					propertySelectors.Add(propertySelector);
					propertySelectorStartIndex = i + 1;
				}
			}

			return propertySelectors;
		}
	}
}
