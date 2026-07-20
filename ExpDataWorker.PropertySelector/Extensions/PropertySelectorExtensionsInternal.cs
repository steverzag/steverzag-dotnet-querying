using System.Buffers;

namespace ExpDataWorker.PropertySelectors.Extensions
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
		internal static SelectorExpressionAnalysis AnalyzePropertySelector(this string expression)
		{
			if (expression.Length == 0)
				return new SelectorExpressionAnalysis(false, []);

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

				if (c == '(')
				{
					parenthesesDepth++;
					continue;
				}
				if (c == ')')
				{
					parenthesesDepth--;
					continue;
				}
				if (parenthesesDepth > 0)
					continue;

				if (mayBeArithmetic && parenthesesDepth == 0 && (c is '+' or '-' or '*' or '/'))
					return new SelectorExpressionAnalysis(true, []);

				if (c == selectorDelimiter)
				{
					parts.Add(expression[partStartIndex..i].Trim());
					partStartIndex = i + 1;
				}
			}

			parts.Add(expression[partStartIndex..].Trim());

			return new SelectorExpressionAnalysis(false, parts);
		}
	}
}
