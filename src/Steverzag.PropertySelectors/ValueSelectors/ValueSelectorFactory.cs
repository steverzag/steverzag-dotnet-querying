using System.Globalization;

namespace Steverzag.PropertySelectors.ValueSelectors
{
	/// <summary>
	/// The literal kinds a selector part can carry.
	/// </summary>
	internal enum ValueLiteralKind
	{
		String,
		DateTime,
		DateOnly,
		TimeOnly,
		Integer,
		Double,
		Boolean
	}

	/// <summary>
	/// A classified literal: the kind recognized in the selector part and its parsed payload.
	/// </summary>
	internal readonly record struct ValueLiteral(ValueLiteralKind Kind, object Value);

	/// <summary>
	/// Recognizes the literal forms a selector part can take (quoted strings, <c>$VAL_*</c>
	/// specifiers and bare numeric or boolean literals) and builds the matching value selector.
	/// Classification lives here once so the generic and non generic selector parsers cannot
	/// drift apart.
	/// </summary>
	internal static class ValueSelectorFactory
	{
		/// <summary>
		/// Builds the non generic value selector for <paramref name="selectorPart"/>, when the
		/// part is a literal at all.
		/// </summary>
		internal static bool TryCreate(string selectorPart, out PropertySelector selector)
		{
			if (!TryClassify(selectorPart, out var literal))
			{
				selector = null!;
				return false;
			}

			selector = literal.Kind switch
			{
				ValueLiteralKind.String => new StringValueSelector((string)literal.Value),
				ValueLiteralKind.DateTime => new DateTimeValueSelector((DateTime)literal.Value),
				ValueLiteralKind.DateOnly => new DateOnlyValueSelector((DateOnly)literal.Value),
				ValueLiteralKind.TimeOnly => new TimeOnlyValueSelector((TimeOnly)literal.Value),
				ValueLiteralKind.Integer => new IntegerValueSelector((int)literal.Value),
				ValueLiteralKind.Double => new DoubleValueSelector((double)literal.Value),
				ValueLiteralKind.Boolean => new BooleanValueSelector((bool)literal.Value),
				_ => throw new NotSupportedException($"The value literal kind '{literal.Kind}' is not supported.")
			};

			return true;
		}

		/// <summary>
		/// Builds the generic value selector for <paramref name="selectorPart"/>, when the part
		/// is a literal at all.
		/// </summary>
		internal static bool TryCreate<T>(string selectorPart, out PropertySelector<T> selector)
		{
			if (!TryClassify(selectorPart, out var literal))
			{
				selector = null!;
				return false;
			}

			selector = literal.Kind switch
			{
				ValueLiteralKind.String => new StringValueSelector<T>((string)literal.Value),
				ValueLiteralKind.DateTime => new DateTimeValueSelector<T>((DateTime)literal.Value),
				ValueLiteralKind.DateOnly => new DateOnlyValueSelector<T>((DateOnly)literal.Value),
				ValueLiteralKind.TimeOnly => new TimeOnlyValueSelector<T>((TimeOnly)literal.Value),
				ValueLiteralKind.Integer => new IntegerValueSelector<T>((int)literal.Value),
				ValueLiteralKind.Double => new DoubleValueSelector<T>((double)literal.Value),
				ValueLiteralKind.Boolean => new BooleanValueSelector<T>((bool)literal.Value),
				_ => throw new NotSupportedException($"The value literal kind '{literal.Kind}' is not supported.")
			};

			return true;
		}

		/// <summary>
		/// Classifies <paramref name="selectorPart"/> as a literal and parses its payload.
		/// Returns false when the part is not a literal, in which case it is a property path.
		/// </summary>
		private static bool TryClassify(string selectorPart, out ValueLiteral literal)
		{
			if (selectorPart.Length > 1 && selectorPart.StartsWith('"') && selectorPart.EndsWith('"'))
			{
				literal = new ValueLiteral(ValueLiteralKind.String, selectorPart[1..^1]);
				return true;
			}

			// $VAL_* payloads are opaque: everything after the prefix belongs to the literal,
			// so characters that are structural elsewhere (dashes, dots, colons) survive.
			if (TryGetPayload(selectorPart, SelectorDefaults.StringValueSpecifier, out var payload))
			{
				literal = new ValueLiteral(ValueLiteralKind.String, payload);
				return true;
			}

			if (TryGetPayload(selectorPart, SelectorDefaults.DateTimeValueSpecifier, out payload))
			{
				literal = new ValueLiteral(ValueLiteralKind.DateTime,
					DateTime.Parse(payload, CultureInfo.InvariantCulture));
				return true;
			}

			if (TryGetPayload(selectorPart, SelectorDefaults.DateOnlyValueSpecifier, out payload))
			{
				literal = new ValueLiteral(ValueLiteralKind.DateOnly,
					DateOnly.Parse(payload, CultureInfo.InvariantCulture));
				return true;
			}

			if (TryGetPayload(selectorPart, SelectorDefaults.TimeOnlyValueSpecifier, out payload))
			{
				literal = new ValueLiteral(ValueLiteralKind.TimeOnly,
					TimeOnly.Parse(payload, CultureInfo.InvariantCulture));
				return true;
			}

			if (TryGetPayload(selectorPart, SelectorDefaults.IntegerValueSpecifier, out payload))
			{
				literal = new ValueLiteral(ValueLiteralKind.Integer,
					int.Parse(payload, CultureInfo.InvariantCulture));
				return true;
			}

			if (TryGetPayload(selectorPart, SelectorDefaults.DoubleValueSpecifier, out payload))
			{
				literal = new ValueLiteral(ValueLiteralKind.Double,
					double.Parse(payload, CultureInfo.InvariantCulture));
				return true;
			}

			if (TryGetPayload(selectorPart, SelectorDefaults.BooleanValueSpecifier, out payload))
			{
				literal = new ValueLiteral(ValueLiteralKind.Boolean, bool.Parse(payload));
				return true;
			}

			// Bare literals carry no specifier, so they keep being read with the current culture.
			if (int.TryParse(selectorPart, out var intValue))
			{
				literal = new ValueLiteral(ValueLiteralKind.Integer, intValue);
				return true;
			}

			if (double.TryParse(selectorPart, out var doubleValue))
			{
				literal = new ValueLiteral(ValueLiteralKind.Double, doubleValue);
				return true;
			}

			if (bool.TryParse(selectorPart, out var boolValue))
			{
				literal = new ValueLiteral(ValueLiteralKind.Boolean, boolValue);
				return true;
			}

			literal = default;
			return false;
		}

		/// <summary>
		/// Matches the <c>specifier + value prefix</c> header and hands back the payload behind it.
		/// Requiring the prefix keeps specifiers that share a start apart, such as
		/// <see cref="SelectorDefaults.DateOnlyValueSpecifier"/> and
		/// <see cref="SelectorDefaults.DateTimeValueSpecifier"/>.
		/// </summary>
		private static bool TryGetPayload(string selectorPart, string specifier, out string payload)
		{
			var headerLength = specifier.Length + SelectorDefaults.ValuePrefix.Length;

			if (selectorPart.Length >= headerLength
				&& selectorPart.StartsWith(specifier, StringComparison.Ordinal)
				&& selectorPart.AsSpan(specifier.Length, SelectorDefaults.ValuePrefix.Length)
					.SequenceEqual(SelectorDefaults.ValuePrefix))
			{
				payload = selectorPart[headerLength..];
				return true;
			}

			payload = string.Empty;
			return false;
		}
	}
}
