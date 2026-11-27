using Steverzag.PropertySelectors.ArithmeticSelectors;
using Steverzag.PropertySelectors.ValueSelectors;

namespace Steverzag.PropertySelectors
{
	internal enum ConstraintMatch
	{
		None,
		Exact,
		Implicit,
		Any,
		Unknown,
	}

	/// <summary>
	/// What a function argument, or the value piped into a function, must be. Used in a <see cref="FunctionSignature"/> to
	/// pick an overload and to report a wrong argument.
	/// </summary>
	/// <remarks>
	/// A nullable type matches like its underlying type. When more than one overload fits, an exact type beats an implicit
	/// conversion or membership of a kind, which beats <see cref="Any"/>.
	/// </remarks>
	public sealed class ArgumentConstraint
	{
		private readonly Func<PropertySelector?, Type?, ConstraintMatch> _match;

		/// <summary>What the constraint expects, as error messages and UIs show it, such as "a number".</summary>
		public string Description { get; }

		/// <summary>
		/// Whether the argument must be written as a literal, as for <see cref="StringLiteral"/> and
		/// <see cref="IntegerLiteral"/>. Such a constraint accepts no property, so a UI should ask for a value instead.
		/// </summary>
		public bool IsLiteral { get; }

		private ArgumentConstraint(string description, Func<PropertySelector?, Type?, ConstraintMatch> match, bool isLiteral = false)
		{
			Description = description;
			_match = match;
			IsLiteral = isLiteral;
		}

		internal ConstraintMatch Match(PropertySelector? argument, Type? type) => _match(argument, type);

		/// <summary>Whether a value of a type fits this constraint, so a UI can offer the properties that fit an argument.</summary>
		/// <param name="type">The value's type.</param>
		/// <returns>
		/// <see langword="true"/> unless the constraint rejects the type. A literal-only constraint (<see cref="IsLiteral"/>)
		/// accepts no type.
		/// </returns>
		public bool Accepts(Type type)
		{
			ArgumentNullException.ThrowIfNull(type);
			return _match(null, type) != ConstraintMatch.None;
		}

		/// <summary>Returns <see cref="Description"/>.</summary>
		public override string ToString() => Description;

		/// <summary>Any value.</summary>
		public static ArgumentConstraint Any { get; } = new("any value", (_, type) => type is null ? ConstraintMatch.Unknown : ConstraintMatch.Any);

		/// <summary>A number of any numeric type. An enum isn't a number.</summary>
		public static ArgumentConstraint Numeric { get; } = Family("a number", type => !type.IsEnum && ArithmeticUtils.IsNumericType(type));

		/// <summary>A number of any integer type, signed or unsigned. An enum isn't an integer.</summary>
		public static ArgumentConstraint Integer { get; } = Family("an integer", IsIntegerType);

		/// <summary>A <see cref="string"/>.</summary>
		public static ArgumentConstraint String { get; } = Of(typeof(string), "a string");

		/// <summary>A <see cref="bool"/>.</summary>
		public static ArgumentConstraint Bool { get; } = Of(typeof(bool), "a boolean");

		/// <summary>A <see cref="DateTime"/>, <see cref="DateTimeOffset"/>, <see cref="DateOnly"/> or <see cref="TimeOnly"/>.</summary>
		public static ArgumentConstraint DateLike { get; } = Family("a date or time",
			type => type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly) || type == typeof(TimeOnly));

		/// <summary>A collection of any element type. A <see cref="string"/> isn't a collection.</summary>
		public static ArgumentConstraint Collection { get; } = new("a collection", (argument, type) => type is null
			? ConstraintMatch.Unknown
			: SelectorExpressions.TryGetEnumerableElementType(type, out var _) ? ConstraintMatch.Implicit : ConstraintMatch.None);

		/// <summary>A collection whose elements are numbers, or nullable numbers.</summary>
		public static ArgumentConstraint NumericCollection { get; } = new("a collection of numbers", (argument, type) =>
		{
			if (type is null)
				return ConstraintMatch.Unknown;

			if (!SelectorExpressions.TryGetEnumerableElementType(type, out var element) || element is null)
				return ConstraintMatch.None;

			var underlying = Nullable.GetUnderlyingType(element) ?? element;
			return !underlying.IsEnum && ArithmeticUtils.IsNumericType(underlying) ? ConstraintMatch.Implicit : ConstraintMatch.None;
		});

		/// <summary>A quoted string written in the selector, such as <c>"2024-01-31"</c>. A property doesn't fit.</summary>
		public static ArgumentConstraint StringLiteral { get; } = new("a quoted string",
			(argument, _) => argument is IStringLiteralSelector ? ConstraintMatch.Exact : ConstraintMatch.None, isLiteral: true);

		/// <summary>An integer written in the selector, such as <c>2</c>. A property doesn't fit.</summary>
		public static ArgumentConstraint IntegerLiteral { get; } = new("an integer literal",
			(argument, _) => argument is IIntegerLiteralSelector ? ConstraintMatch.Exact : ConstraintMatch.None, isLiteral: true);

		/// <summary>
		/// A value of one type, or of a numeric type that converts to it implicitly as in C# (an <see cref="int"/> for a
		/// <see cref="long"/>).
		/// </summary>
		/// <param name="expected">The type.</param>
		/// <param name="description">What error messages and UIs show, such as "a price". By default, the type's name with an article.</param>
		/// <returns>The constraint.</returns>
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

		/// <summary>A value of any type a predicate accepts, such as every numeric type.</summary>
		/// <param name="description">What error messages and UIs show, such as "a number".</param>
		/// <param name="isMember">Returns whether a type belongs to the kind. It receives the underlying type of a nullable.</param>
		/// <returns>The constraint.</returns>
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
