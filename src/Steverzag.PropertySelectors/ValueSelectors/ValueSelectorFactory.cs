using System.Globalization;
using System.Text.RegularExpressions;

namespace Steverzag.PropertySelectors.ValueSelectors
{
	internal static partial class ValueSelectorFactory
	{
		[GeneratedRegex(@"^-?\d+(?<fraction>\.\d+)?(?<exponent>[eE][+-]?\d+)?(?<suffix>[lLmM])?$", RegexOptions.CultureInvariant)]
		private static partial Regex NumberLiteral();

		internal static bool IsNumberLiteral(string text) => NumberLiteral().IsMatch(text);

		internal static bool TryCreate(string selectorPart, out PropertySelector selector)
		{
			if (!TryClassify(selectorPart, out var value))
			{
				selector = null!;
				return false;
			}

			selector = value switch
			{
				string s => new StringValueSelector(s),
				bool b => new BooleanValueSelector(b),
				int i => new IntegerValueSelector(i),
				long l => new LongValueSelector(l),
				double d => new DoubleValueSelector(d),
				decimal m => new DecimalValueSelector(m),
				_ => throw new NotSupportedException($"The literal type '{value.GetType().Name}' is not supported.")
			};
			return true;
		}

		internal static bool TryCreate<T>(string selectorPart, out PropertySelector<T> selector)
		{
			if (!TryClassify(selectorPart, out var value))
			{
				selector = null!;
				return false;
			}

			selector = value switch
			{
				string s => new StringValueSelector<T>(s),
				bool b => new BooleanValueSelector<T>(b),
				int i => new IntegerValueSelector<T>(i),
				long l => new LongValueSelector<T>(l),
				double d => new DoubleValueSelector<T>(d),
				decimal m => new DecimalValueSelector<T>(m),
				_ => throw new NotSupportedException($"The literal type '{value.GetType().Name}' is not supported.")
			};
			return true;
		}

		private static bool TryClassify(string selectorPart, out object value)
		{
			if (selectorPart.Length > 1 && selectorPart[0] == '"' && selectorPart[^1] == '"')
			{
				value = ParseString(selectorPart);
				return true;
			}

			if (bool.TryParse(selectorPart, out var boolValue))
			{
				value = boolValue;
				return true;
			}

			var match = NumberLiteral().Match(selectorPart);
			if (match.Success)
			{
				value = ParseNumber(selectorPart, match);
				return true;
			}

			value = null!;
			return false;
		}

		private static string ParseString(string literal)
		{
			var body = literal[1..^1];

			if (body.Replace("\"\"", string.Empty).Contains('"'))
				throw new ArgumentException(
					$"Invalid string literal {literal}: a '\"' inside a string must be doubled (\"\").", nameof(literal));

			return body.Replace("\"\"", "\"");
		}

		private static object ParseNumber(string literal, Match match)
		{
			var suffix = match.Groups["suffix"];
			var number = suffix.Success ? literal[..^1] : literal;
			var isInteger = !match.Groups["fraction"].Success && !match.Groups["exponent"].Success;

			switch (suffix.Success ? char.ToLowerInvariant(suffix.Value[0]) : '\0')
			{
				case 'm':
					return decimal.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var decimalValue)
						? decimalValue
						: throw OutOfRange(literal, "decimal");

				case 'l':
					if (!isInteger)
						throw new ArgumentException($"Invalid literal '{literal}': the 'L' suffix needs an integer.", nameof(literal));
					return long.TryParse(number, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var longValue)
						? longValue
						: throw OutOfRange(literal, "long");
			}

			if (!isInteger)
			{
				var doubleValue = double.Parse(number, NumberStyles.Float, CultureInfo.InvariantCulture);
				return double.IsFinite(doubleValue) ? doubleValue : throw OutOfRange(literal, "double");
			}

			if (int.TryParse(number, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var intValue))
				return intValue;

			return long.TryParse(number, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var wideValue)
				? wideValue
				: throw OutOfRange(literal, "long");
		}

		private static ArgumentException OutOfRange(string literal, string type)
			=> new($"The literal '{literal}' does not fit in a {type}.", "selectorPart");
	}
}
