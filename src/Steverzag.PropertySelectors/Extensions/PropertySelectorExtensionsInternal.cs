using Steverzag.PropertySelectors.ValueSelectors;
using System.Buffers;

namespace Steverzag.PropertySelectors.Extensions
{
	/// <summary>
	/// Result of a single scan over a selector string: whether it is a top-level arithmetic
	/// expression and, when it is not, its parts split on the top-level property delimiter.
	/// </summary>
	internal readonly record struct SelectorExpressionAnalysis(bool IsTopLevelArithmetic, List<string> Parts);

	internal static class PropertySelectorExtensionsInternal
	{
		private static readonly SearchValues<char> ArithmeticOperatorChars = SearchValues.Create("+-*/");

		/// <summary>
		/// Classifies <paramref name="expression"/> in a single pass: detects an arithmetic
		/// operator outside parentheses and, when none is found, splits the expression on
		/// top-level <see cref="SelectorDefaults.PropertyDelimiter"/> occurrences.
		/// </summary>
		/// <exception cref="ArgumentException">
		/// The parentheses of <paramref name="expression"/> do not balance. Both checks exist so
		/// that malformed input fails as a syntax error here instead of being reclassified as a
		/// property path: an unbalanced depth makes the scan skip the very operators and
		/// delimiters it is looking for, and the whole string would then be looked up as a
		/// single property name.
		/// </exception>
		internal static SelectorExpressionAnalysis AnalyzePropertySelector(this string expression)
		{
			if (expression.Length == 0)
				return new SelectorExpressionAnalysis(false, []);

			if (ValueSelectorFactory.IsNumberLiteral(expression.Trim()))
				return new SelectorExpressionAnalysis(false, [expression.Trim()]);

			// Vectorized pre-check: without any operator character the expression cannot be
			// arithmetic, so the per-character operator test below is skipped entirely.
			var mayBeArithmetic = expression.AsSpan().ContainsAny(ArithmeticOperatorChars);

			var parts = new List<string>();
			var parenthesesDepth = 0;
			var partStartIndex = 0;
			var selectorDelimiter = SelectorDefaults.PropertyDelimiter[0];

			for (int i = 0; i < expression.Length; i++)
			{
				var c = expression[i];

				if (c == '"')
				{
					i = expression.EndOfQuotedLiteral(i);
					continue;
				}

				if (c == '(')
				{
					parenthesesDepth++;
					continue;
				}
				if (c == ')')
				{
					// A stray ')' would drive the depth negative, which passes the "> 0" guard
					// below but fails the "depth is zero" premise the operator test relies on,
					// silently disabling detection for the rest of the scan.
					if (parenthesesDepth == 0)
						throw new ArgumentException(
							$"Unbalanced parentheses in selector '{expression}': unexpected ')' at index {i}.",
							nameof(expression));

					parenthesesDepth--;
					continue;
				}
				if (parenthesesDepth > 0)
					continue;

				// Past the guards the depth is always exactly zero.
				if (mayBeArithmetic && (c is '+' or '-' or '*' or '/'))
					return new SelectorExpressionAnalysis(true, []);

				if (c == selectorDelimiter)
				{
					parts.Add(expression[partStartIndex..i].Trim());
					partStartIndex = i + 1;
				}
			}

			// An unclosed '(' swallows every operator and delimiter after it.
			if (parenthesesDepth != 0)
				throw new ArgumentException(
					$"Unbalanced parentheses in selector '{expression}': {parenthesesDepth} unclosed '('.",
					nameof(expression));

			parts.Add(expression[partStartIndex..].Trim());

			return new SelectorExpressionAnalysis(false, parts);
		}

		internal static int EndOfQuotedLiteral(this string text, int openingQuoteIndex)
		{
			var index = openingQuoteIndex + 1;

			while (true)
			{
				var closingQuoteIndex = text.IndexOf('"', index);
				if (closingQuoteIndex == -1)
					return text.Length - 1;

				if (closingQuoteIndex + 1 < text.Length && text[closingQuoteIndex + 1] == '"')
				{
					index = closingQuoteIndex + 2;
					continue;
				}

				return closingQuoteIndex;
			}
		}
	}
}
