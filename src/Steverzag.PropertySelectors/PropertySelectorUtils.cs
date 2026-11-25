using Steverzag.PropertySelectors.ArithmeticSelectors;
using Steverzag.PropertySelectors.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steverzag.PropertySelectors
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
			var arithmeticOperationSelector = parser.Parse();

			return arithmeticOperationSelector;
		}

		internal static bool TryGetParameters(string selector, Func<string, PropertySelector> parse, out List<PropertySelector> parameters)
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
			var parameterStartIndex = 0;
			for (var i = 0; i < innerParameters.Length; i++)
			{
				var ch = innerParameters[i];
				if (ch == '"')
				{
					i = innerParameters.EndOfQuotedLiteral(i);
				}
				else if (ch == '(')
				{
					nestedParenthesesCount++;
				}
				else if (ch == ')')
				{
					nestedParenthesesCount--;
				}
				else if (nestedParenthesesCount == 0 && ch == ',')
				{
					var parameter = innerParameters[parameterStartIndex..i].Trim();
					if (!string.IsNullOrEmpty(parameter))
					{
						var propertySelector = parse(parameter);
						result.Add(propertySelector);
					}
					parameterStartIndex = i + 1;
				}
			}

			var lastParameter = innerParameters[parameterStartIndex..].Trim();
			if (!string.IsNullOrEmpty(lastParameter))
			{
				var propertySelector = parse(lastParameter);
				result.Add(propertySelector);
			}

			parameters = result;

			return parameters.Count > 0;
		}
	}
}
