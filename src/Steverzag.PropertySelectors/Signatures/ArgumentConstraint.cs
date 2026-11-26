using Steverzag.PropertySelectors.ArithmeticSelectors;
using Steverzag.PropertySelectors.ValueSelectors;

namespace Steverzag.PropertySelectors
{
	public enum ConstraintMatch
	{
		None,
		Exact,
		Implicit,
		Any,
		Unknown,
	}

	public sealed class ArgumentConstraint
	{
		private readonly Func<PropertySelector?, Type?, ConstraintMatch> _match;

		public string Description { get; }

		public bool IsLiteral { get; }

		private ArgumentConstraint(string description, Func<PropertySelector?, Type?, ConstraintMatch> match, bool isLiteral = false)
		{
			Description = description;
			_match = match;
			IsLiteral = isLiteral;
		}

		public ConstraintMatch Match(PropertySelector? argument, Type? type) => _match(argument, type);

		public bool Accepts(Type type)
		{
			ArgumentNullException.ThrowIfNull(type);
			return _match(null, type) != ConstraintMatch.None;
		}

		public override string ToString() => Description;

		public static ArgumentConstraint Any { get; } = new("any value", (_, type) => type is null ? ConstraintMatch.Unknown : ConstraintMatch.Any);

		public static ArgumentConstraint Numeric { get; } = Family("a number", type => !type.IsEnum && ArithmeticUtils.IsNumericType(type));

		public static ArgumentConstraint Integer { get; } = Family("an integer", IsIntegerType);

		public static ArgumentConstraint String { get; } = Of(typeof(string), "a string");

		public static ArgumentConstraint Bool { get; } = Of(typeof(bool), "a boolean");

		public static ArgumentConstraint DateLike { get; } = Family("a date or time",
			type => type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly) || type == typeof(TimeOnly));

		public static ArgumentConstraint Collection { get; } = new("a collection", (argument, type) => type is null
			? ConstraintMatch.Unknown
			: ExpressionUtils.TryGetEnumerableElementType(type, out var _) ? ConstraintMatch.Implicit : ConstraintMatch.None);

		public static ArgumentConstraint NumericCollection { get; } = new("a collection of numbers", (argument, type) =>
		{
			if (type is null)
				return ConstraintMatch.Unknown;

			if (!ExpressionUtils.TryGetEnumerableElementType(type, out var element) || element is null)
				return ConstraintMatch.None;

			var underlying = Nullable.GetUnderlyingType(element) ?? element;
			return !underlying.IsEnum && ArithmeticUtils.IsNumericType(underlying) ? ConstraintMatch.Implicit : ConstraintMatch.None;
		});

		public static ArgumentConstraint StringLiteral { get; } = new("a quoted string",
			(argument, _) => argument is IStringLiteralSelector ? ConstraintMatch.Exact : ConstraintMatch.None, isLiteral: true);

		public static ArgumentConstraint IntegerLiteral { get; } = new("an integer literal",
			(argument, _) => argument is IIntegerLiteralSelector ? ConstraintMatch.Exact : ConstraintMatch.None, isLiteral: true);

		public static ArgumentConstraint Of(Type expected, string? description = null) =>
			new(description ?? TypeNames.WithArticle(expected), (_, type) =>
			{
				if (type is null)
					return ConstraintMatch.Unknown;

				var underlying = Nullable.GetUnderlyingType(type) ?? type;
				if (underlying == expected)
					return ConstraintMatch.Exact;

				return ImplicitNumericConversions.TryGetValue(underlying, out var targets) && targets.Contains(expected)
					? ConstraintMatch.Implicit
					: ConstraintMatch.None;
			});

		public static ArgumentConstraint Family(string description, Func<Type, bool> isMember) =>
			new(description, (_, type) =>
			{
				if (type is null)
					return ConstraintMatch.Unknown;

				return isMember(Nullable.GetUnderlyingType(type) ?? type) ? ConstraintMatch.Implicit : ConstraintMatch.None;
			});

		private static bool IsIntegerType(Type type) => !type.IsEnum && Type.GetTypeCode(type) switch
		{
			TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32
				or TypeCode.Int64 or TypeCode.UInt64 => true,
			_ => false,
		};

		private static readonly Dictionary<Type, Type[]> ImplicitNumericConversions = new()
		{
			[typeof(sbyte)] = [typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)],
			[typeof(byte)] = [typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
			[typeof(short)] = [typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)],
			[typeof(ushort)] = [typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
			[typeof(int)] = [typeof(long), typeof(float), typeof(double), typeof(decimal)],
			[typeof(uint)] = [typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
			[typeof(long)] = [typeof(float), typeof(double), typeof(decimal)],
			[typeof(ulong)] = [typeof(float), typeof(double), typeof(decimal)],
			[typeof(float)] = [typeof(double)],
		};
	}

	internal static class TypeNames
	{
		internal static string WithArticle(Type type)
		{
			var name = Describe(type);
			return "aeiouAEIOU".Contains(name[0]) ? $"an {name}" : $"a {name}";
		}

		internal static string Describe(Type type)
		{
			if (Nullable.GetUnderlyingType(type) is { } underlying)
				return Describe(underlying) + "?";

			if (!type.IsGenericType)
				return type.Name;

			var name = type.Name[..type.Name.IndexOf('`')];
			return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(Describe))}>";
		}
	}
}
