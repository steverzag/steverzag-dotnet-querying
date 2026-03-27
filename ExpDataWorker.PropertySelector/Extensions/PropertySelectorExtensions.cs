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
			var parenthesesDepth = 0;
			var propertySelectorStartIndex = 0;
			var selectorDelimiter = SelectorDefaults.PropertyDelimiter[0];

			for (int i = 0; i < expression.Length; i++)
			{
				if (i == expression.Length - 1)
				{
					var propertySelector = expression[propertySelectorStartIndex..].Trim();
					propertySelectors.Add(propertySelector);
				}
				if (expression[i] == '(')
				{
					parenthesesDepth++;
					continue;
				}
				else if (expression[i] == ')')
				{
					parenthesesDepth--;
					continue;
				}
				else if (parenthesesDepth > 0)
					continue;
				else if (expression[i] == selectorDelimiter)
				{
					var propertySelector = expression[propertySelectorStartIndex..i].Trim();
					propertySelectors.Add(propertySelector);
					propertySelectorStartIndex = i + 1;
				}
			}

			return propertySelectors;
		}

		internal static bool IsArithmeticExpressionTopLevel(this string expression)
		{
			if (string.IsNullOrWhiteSpace(expression))
				return false;

			int depth = 0;

			for (int i = 0; i < expression.Length; i++)
			{
				var c = expression[i];

				if (c == '(')
				{
					depth++;
					continue;
				}

				if (c == ')')
				{
					depth--;
					continue;
				}

				if (depth == 0 && (c == '+' || c == '-' || c == '*' || c == '/'))
				{
					return true;
				}
			}

			return false;
		}
	}
}
