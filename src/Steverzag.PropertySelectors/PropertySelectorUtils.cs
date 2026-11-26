using Steverzag.PropertySelectors.ArithmeticSelectors;
using Steverzag.PropertySelectors.Extensions;
using Steverzag.PropertySelectors.Registration;
using System.Collections.Concurrent;
using System.Reflection;

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

		private static readonly ConcurrentDictionary<Type, Func<string, PropertySelector>> PropertySegmentFactories = new();

		private static readonly MethodInfo CreateTypedPropertySegmentMethod =
			typeof(PropertySelectorUtils).GetMethod(nameof(CreateTypedPropertySegment), BindingFlags.NonPublic | BindingFlags.Static)!;

		private static PropertySelector CreateTypedPropertySegment<TScope>(string selectorPart) => new PropertySelector<TScope>(selectorPart);

		internal static PropertySelector CreatePropertySegment(string selectorPart, Type? pipedType)
		{
			if (pipedType is null)
				return new PropertySelector(selectorPart);

			var factory = PropertySegmentFactories.GetOrAdd(pipedType, static type =>
				CreateTypedPropertySegmentMethod.MakeGenericMethod(type).CreateDelegate<Func<string, PropertySelector>>());

			return factory(selectorPart);
		}

		internal static SelectorParseException UnknownFunction(string selectorPart)
		{
			var name = FunctionName(selectorPart);

			var trimmed = name.TrimEnd();
			if (trimmed.Length < name.Length && selectorPart.Length > name.Length)
				return new SelectorParseException(
					$"No space is allowed between a function and its '(': write '{trimmed}('.", selectorPart, trimmed.Length, name[trimmed.Length..]);

			var dot = name.IndexOf('.');
			if (dot > 0 && SelectorRegistry.IsRegistered(name[..dot]))
				return MemberAfterFunction(selectorPart, name[..dot], name[(dot + 1)..], dot);

			var differentlyCased = SelectorRegistry.FindTokenIgnoringCase(name);
			var hint = differentlyCased is null ? string.Empty : $" Function names are case-sensitive: '{differentlyCased}'.";

			return new SelectorParseException($"Unknown function '{name}'.{hint}", selectorPart, 0, name);
		}

		internal static void ValidatePropertyPath(string path)
		{
			if (path.Length > 0 && (char.IsDigit(path[0]) || (path[0] == '.' && path.Length > 1 && char.IsDigit(path[1]))))
				throw new SelectorParseException(MalformedNumber(path), path, 0, path);

			var segmentStart = 0;
			for (var i = 0; i <= path.Length; i++)
			{
				if (i < path.Length && path[i] != '.')
					continue;

				ValidateName(path, segmentStart, i);
				segmentStart = i + 1;
			}
		}

		private static void ValidateName(string path, int start, int end)
		{
			if (start == end)
				throw new SelectorParseException($"Empty property name: '.' must sit between two names.", path, start == path.Length ? start - 1 : start, ".");

			var nameStart = start;
			if (path[start] == SelectorDefaults.PropertyNameEscape)
			{
				nameStart++;
				if (nameStart == end || path[nameStart] == SelectorDefaults.PropertyNameEscape)
					throw new SelectorParseException($"'{SelectorDefaults.PropertyNameEscape}' must be followed by a property name.", path, start, SelectorDefaults.PropertyNameEscape.ToString());
			}

			if (char.IsDigit(path[nameStart]))
				throw new SelectorParseException("A property name can't start with a digit.", path, nameStart, path[nameStart..end]);

			for (var i = nameStart; i < end; i++)
			{
				var c = path[i];
				if (i == nameStart ? IsNameStart(c) : IsNamePart(c))
					continue;

				if (c == '(')
					throw new SelectorParseException("'(' can't follow a property name: only a function ('$FN_…') takes arguments.", path, i, "(");

				if (char.IsWhiteSpace(c))
					throw new SelectorParseException("A property name can't contain whitespace.", path, i, c.ToString());

				throw new SelectorParseException($"Unexpected character '{c}' in a property name.", path, i, c.ToString());
			}
		}

		private static bool IsNameStart(char c) => char.IsLetter(c) || c == '_';

		private static bool IsNamePart(char c) => char.IsLetterOrDigit(c) || c == '_' || char.GetUnicodeCategory(c) is
			System.Globalization.UnicodeCategory.NonSpacingMark or System.Globalization.UnicodeCategory.SpacingCombiningMark
			or System.Globalization.UnicodeCategory.ConnectorPunctuation or System.Globalization.UnicodeCategory.Format;

		private static string MalformedNumber(string text)
		{
			var hint = text.StartsWith('.') || text.EndsWith('.')
				? " A '.' needs a digit on both sides ('0.5', '1.0')."
				: text.EndsWith('e') || text.EndsWith('E')
					? " An exponent needs digits after 'e' ('1e3')."
					: string.Empty;

			return $"'{text}' isn't a valid number.{hint}";
		}

		internal static SelectorParseException LiteralAfterPipe(string selectorPart)
			=> new($"A literal can't follow '{SelectorDefaults.PropertyDelimiter}': it would discard the value before it.", selectorPart, 0, selectorPart);

		internal static SelectorParseException MemberAfterFunction(string selectorPart, string function, string member, int dotPosition)
			=> new($"A member can't follow a function with '.'. Write it after a pipe: '{function}{SelectorDefaults.PropertyDelimiter}{member}'.", selectorPart, dotPosition, ".");

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
			if (trailing.StartsWith('.') && trailing.Length > 1)
				throw MemberAfterFunction(selector, selector[..(endingParenthesesIndex + 1)], trailing[1..].Trim(), endingParenthesesIndex + 1);

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
