using Steverzag.PropertySelectors.ArithmeticSelectors;
using Steverzag.PropertySelectors.Extensions;

namespace Steverzag.PropertySelectors
{
	public class PropertySelectorUtils
	{
		internal static PropertySelector<T> CreateArithmeticOperationSelector<T>(string selector)
		{
			var tokenizer = new ArithmeticOperationTokenizer(selector);
			var tokens = tokenizer.Tokenize();
			tokens = tokenizer.SanitizeNegations(tokens);

			var parser = new ArithmeticOperationParser(selector, tokens);
			return parser.Parse<T>();
		}

		internal static PropertySelector CreateArithmeticOperationSelector(string selector)
		{
			var tokenizer = new ArithmeticOperationTokenizer(selector);
			var tokens = tokenizer.Tokenize();
			tokens = tokenizer.SanitizeNegations(tokens);

			var parser = new ArithmeticOperationParser(selector, tokens);
			return parser.Parse();
		}

		internal static PropertySelector ParseArithmetic(string selector, Func<PropertySelector> parse)
		{
			try
			{
				return parse();
			}
			catch (Exception e) when (e is not SelectorParseException && SelectorParseException.IsSelectorError(e))
			{
				throw SelectorParseException.From(e, selector, 0, selector);
			}
		}

		internal static PropertySelector ParsePart(string selector, string part, int offset, Func<PropertySelector> parse)
		{
			try
			{
				return parse();
			}
			catch (Exception e) when (SelectorParseException.IsSelectorError(e))
			{
				throw SelectorParseException.From(e, selector, offset, part);
			}
		}

		internal static SelectorParseException UnknownFunction(string selectorPart)
		{
			var name = FunctionName(selectorPart);
			return new SelectorParseException($"Unknown function '{name}'.", selectorPart, 0, name);
		}

		internal static string FunctionName(string selectorPart)
		{
			var parenthesesIndex = selectorPart.IndexOf('(');
			return parenthesesIndex < 0 ? selectorPart : selectorPart[..parenthesesIndex];
		}

		internal static bool TryGetParameters(string selector, Func<string, int, PropertySelector> parse, out List<PropertySelector> parameters)
		{
			var startingParenthesesIndex = selector.IndexOf('(');
			if (startingParenthesesIndex is -1)
			{
				parameters = [];
				return false;
			}

			var endingParenthesesIndex = selector.LastIndexOf(')');
			if (endingParenthesesIndex < startingParenthesesIndex)
				throw new SelectorParseException("Unbalanced parentheses: 1 unclosed '('.", selector, startingParenthesesIndex, "(");

			var trailing = selector[(endingParenthesesIndex + 1)..];
			if (!string.IsNullOrWhiteSpace(trailing))
				throw new SelectorParseException($"Unexpected '{trailing.Trim()}' after the closing ')'.", selector, endingParenthesesIndex + 1 + (trailing.Length - trailing.TrimStart().Length), trailing.Trim());

			var argumentsStart = startingParenthesesIndex + 1;
			var innerParameters = selector[argumentsStart..endingParenthesesIndex];

			var segments = new List<(int Start, int End)>();
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
					segments.Add((parameterStartIndex, i));
					parameterStartIndex = i + 1;
				}
			}
			segments.Add((parameterStartIndex, innerParameters.Length));

			var result = new List<PropertySelector>();
			if (segments.Count == 1 && string.IsNullOrWhiteSpace(innerParameters))
			{
				parameters = result;
				return false;
			}

			foreach (var (start, end) in segments)
			{
				var raw = innerParameters[start..end];
				var text = raw.Trim();
				var offset = argumentsStart + start + (raw.Length - raw.TrimStart().Length);

				if (text.Length == 0)
					throw new SelectorParseException($"Empty argument {result.Count + 1}.", selector, argumentsStart + start, ",");

				result.Add(parse(text, offset));
			}

			parameters = result;
			return true;
		}

		internal static PropertySelector ParseArgument(Func<string, PropertySelector> parse, string selectorPart, string text, int offset)
		{
			try
			{
				return parse(text);
			}
			catch (Exception e) when (SelectorParseException.IsSelectorError(e))
			{
				throw SelectorParseException.From(e, selectorPart, offset, text);
			}
		}
	}
}
