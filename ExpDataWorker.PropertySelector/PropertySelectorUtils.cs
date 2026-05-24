using ExpDataWorker.PropertySelectors.ArithmeticSelectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelectors
{
	public class PropertySelectorUtils
	{
		internal static PropertySelector<T> CreateArithmeticOperationSelector<T>(string selector)
		{
			var tokenizer = new ArithmeticOperationTokenizer(selector);
			var tokens = tokenizer.Tokenize();
			tokens = tokenizer.SanitizeNegations(tokens);

			var parser = new ArithmeticOperationParser(tokens);
			var arithmeticOperationSelector = parser.Parse<T>();

			return arithmeticOperationSelector;
		}

		internal static PropertySelector CreateArithmeticOperationSelector(string selector)
		{
			var tokenizer = new ArithmeticOperationTokenizer(selector);
			var tokens = tokenizer.Tokenize();
			tokens = tokenizer.SanitizeNegations(tokens);

			var parser = new ArithmeticOperationParser(tokens);
			var arithmeticOperationSelector = parser.Parse<object>();

			return arithmeticOperationSelector;
		}

		internal static bool TryGetParameters(string selector, out List<PropertySelector> parameters)
		{
			var startingParenthesesIndex = selector.IndexOf('(');
			if (startingParenthesesIndex is -1)
			{
				parameters = [];
				return false;
			}

			var result = new List<PropertySelector>();
			var endingParenthesesIndex = selector.LastIndexOf(')');

			var innerParameters = selector.Substring(startingParenthesesIndex + 1,
				endingParenthesesIndex - startingParenthesesIndex - 1);

			var nestedParenthesesCount = 0;
			var lastParameterEndIndex = 0;
			for (var i = 0; i < innerParameters.Length; i++)
			{
				var ch = innerParameters[i];
				if (ch == '(')
				{
					nestedParenthesesCount++;
					continue;
				}
				else if (ch == ')')
				{
					nestedParenthesesCount--;
					continue;
				}
				else if (nestedParenthesesCount == 0 && ch == ',')
				{
					var startIndex = lastParameterEndIndex is 0
						? 0
						: lastParameterEndIndex + 1;

					var parameter = innerParameters.Substring(startIndex, i - startIndex).Trim();
					if (!string.IsNullOrEmpty(parameter))
					{
						var propertySelector = (PropertySelector)parameter;
						result.Add(propertySelector);
						lastParameterEndIndex = i + 1;
					}
				}
				else if (nestedParenthesesCount == 0 && i == innerParameters.Length - 1)
				{
					var parameter = innerParameters.Substring(lastParameterEndIndex).Trim();
					if (!string.IsNullOrEmpty(parameter))
					{
						var propertySelector = (PropertySelector)parameter;
						result.Add(propertySelector);
					}
				}
			}

			parameters = result;

			return parameters.Count > 0;
		}
	}
}
