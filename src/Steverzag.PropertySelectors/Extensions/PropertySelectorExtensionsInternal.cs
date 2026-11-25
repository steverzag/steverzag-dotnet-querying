using Steverzag.PropertySelectors.ValueSelectors;
using System.Buffers;

namespace Steverzag.PropertySelectors.Extensions
{
	internal readonly record struct SelectorPartText(string Text, int Offset);

	internal readonly record struct SelectorExpressionAnalysis(bool IsTopLevelArithmetic, List<SelectorPartText> Parts);

	internal static class PropertySelectorExtensionsInternal
	{
		private static readonly SearchValues<char> ArithmeticOperatorChars = SearchValues.Create("+-*/");

		internal static SelectorExpressionAnalysis AnalyzePropertySelector(this string expression)
		{
			if (expression.Length == 0)
				return new SelectorExpressionAnalysis(false, []);

			if (ValueSelectorFactory.IsNumberLiteral(expression.Trim()))
				return new SelectorExpressionAnalysis(false, [Trimmed(expression, 0, expression.Length)]);

			var mayBeArithmetic = expression.AsSpan().ContainsAny(ArithmeticOperatorChars);

			var parts = new List<SelectorPartText>();
			var parenthesesDepth = 0;
			var lastOpenParenthesis = -1;
			var partStartIndex = 0;
			var selectorDelimiter = SelectorDefaults.PropertyDelimiter[0];

			for (int i = 0; i < expression.Length; i++)
			{
				var c = expression[i];

				if (c == '"')
				{
					i = expression.EndOfQuotedLiteralOrThrow(i);
					continue;
				}

				if (c == '(')
				{
					parenthesesDepth++;
					lastOpenParenthesis = i;
					continue;
				}
				if (c == ')')
				{
					if (parenthesesDepth == 0)
						throw new SelectorParseException("Unbalanced parentheses: unexpected ')'.", expression, i, ")");

					parenthesesDepth--;
					continue;
				}
				if (parenthesesDepth > 0)
					continue;

				if (mayBeArithmetic && (c is '+' or '-' or '*' or '/'))
					return new SelectorExpressionAnalysis(true, []);

				if (c == selectorDelimiter)
				{
					parts.Add(Part(expression, partStartIndex, i));
					partStartIndex = i + 1;
				}
			}

			if (parenthesesDepth != 0)
				throw new SelectorParseException($"Unbalanced parentheses: {parenthesesDepth} unclosed '('.", expression, lastOpenParenthesis, "(");

			parts.Add(Part(expression, partStartIndex, expression.Length));

			return new SelectorExpressionAnalysis(false, parts);
		}

		private static SelectorPartText Part(string expression, int start, int end)
		{
			var part = Trimmed(expression, start, end);
			if (part.Text.Length == 0)
				throw new SelectorParseException($"Empty part: '{SelectorDefaults.PropertyDelimiter}' must sit between two parts.", expression, start, SelectorDefaults.PropertyDelimiter);

			return part;
		}

		private static SelectorPartText Trimmed(string expression, int start, int end)
		{
			while (start < end && char.IsWhiteSpace(expression[start]))
				start++;
			while (end > start && char.IsWhiteSpace(expression[end - 1]))
				end--;

			return new SelectorPartText(expression[start..end], start);
		}

		internal static int EndOfQuotedLiteral(this string text, int openingQuoteIndex)
			=> text.TryFindEndOfQuotedLiteral(openingQuoteIndex, out var end) ? end : text.Length - 1;

		internal static int EndOfQuotedLiteralOrThrow(this string text, int openingQuoteIndex)
			=> text.TryFindEndOfQuotedLiteral(openingQuoteIndex, out var end)
				? end
				: throw new SelectorParseException("Unterminated string literal: a '\"' is never closed.", text, openingQuoteIndex, text[openingQuoteIndex..]);

		private static bool TryFindEndOfQuotedLiteral(this string text, int openingQuoteIndex, out int end)
		{
			var index = openingQuoteIndex + 1;

			while (true)
			{
				var closingQuoteIndex = text.IndexOf('"', index);
				if (closingQuoteIndex == -1)
				{
					end = -1;
					return false;
				}

				if (closingQuoteIndex + 1 < text.Length && text[closingQuoteIndex + 1] == '"')
				{
					index = closingQuoteIndex + 2;
					continue;
				}

				end = closingQuoteIndex;
				return true;
			}
		}
	}
}
