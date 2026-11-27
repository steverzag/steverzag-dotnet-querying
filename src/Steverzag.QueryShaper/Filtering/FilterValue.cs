using System.Text.Json.Serialization;
using Steverzag.QueryShaper.Serialization;
using System.Runtime.CompilerServices;

namespace Steverzag.QueryShaper.Filtering
{
	/// <summary>
	/// The value of a filter condition: a single value of a supported type, or a list of them. It is converted to the
	/// property's type when the filter is built.
	/// </summary>
	/// <remarks>
	/// Build one from a .NET value with an implicit conversion (<c>Value = 10</c>, <c>Value = "2026-11-27"</c>). From JSON
	/// it binds from a string, a number, a boolean, <c>null</c> or a list of those. How each kind is converted to the
	/// property's type is described in "Filter values" in <c>docs/query-shaper.md</c>.
	/// </remarks>
	[JsonConverter(typeof(FilterValueJsonConverter))]
	public readonly struct FilterValue
	{
		private readonly bool? _boolValue;
		private readonly long? _integerValue;
		private readonly double? _doubleValue;
		private readonly decimal? _decimalValue;
		private readonly char? _charValue;
		private readonly string? _stringValue;
		private readonly Guid? _guidValue;
		private readonly DateTime? _dateTimeValue;
		private readonly DateOnly? _dateOnlyValue;
		private readonly TimeOnly? _timeOnlyValue;
		private readonly TimeSpan? _timeSpanValue;
		private readonly DateTimeOffset? _dateTimeOffset;
		private readonly List<object>? _valuesList;

		private readonly FilterValueKind _kind;

		private FilterValue(Type type)
		{
			_kind = type switch
			{
				Type t when t == typeof(bool) => FilterValueKind.Bool,
				Type t when t == typeof(byte) => FilterValueKind.Integer,
				Type t when t == typeof(short) => FilterValueKind.Integer,
				Type t when t == typeof(int) => FilterValueKind.Integer,
				Type t when t == typeof(long) => FilterValueKind.Integer,
				Type t when t == typeof(float) => FilterValueKind.Double,
				Type t when t == typeof(double) => FilterValueKind.Double,
				Type t when t == typeof(decimal) => FilterValueKind.Decimal,
				Type t when t == typeof(char) => FilterValueKind.Char,
				Type t when t == typeof(string) => FilterValueKind.String,
				Type t when t == typeof(Guid) => FilterValueKind.Guid,
				Type t when t == typeof(DateTime) => FilterValueKind.DateTime,
				Type t when t == typeof(DateOnly) => FilterValueKind.DateOnly,
				Type t when t == typeof(TimeOnly) => FilterValueKind.TimeOnly,
				Type t when t == typeof(TimeSpan) => FilterValueKind.TimeSpan,
				Type t when t == typeof(DateTimeOffset) => FilterValueKind.DateTimeOffset,
				Type t when t == typeof(List<>) => FilterValueKind.Enumerable,
				_ => throw new ArgumentException($"No value kind for type {type.Name}.", nameof(type))
			};
		}

		/// <summary>Returns the kind of value held.</summary>
		/// <returns>The kind. A value built from a <see cref="byte"/>, <see cref="short"/>, <see cref="int"/> or <see cref="long"/> is <see cref="FilterValueKind.Integer"/>, and one built from a <see cref="float"/> is <see cref="FilterValueKind.Double"/>.</returns>
		public FilterValueKind GetKind() => _kind;

		internal object? ToObject() => _kind switch
		{
			FilterValueKind.Bool => _boolValue,
			FilterValueKind.Integer => _integerValue,
			FilterValueKind.Double => _doubleValue,
			FilterValueKind.Decimal => _decimalValue,
			FilterValueKind.Char => _charValue,
			FilterValueKind.String => _stringValue,
			FilterValueKind.Guid => _guidValue,
			FilterValueKind.DateTime => _dateTimeValue,
			FilterValueKind.DateOnly => _dateOnlyValue,
			FilterValueKind.TimeOnly => _timeOnlyValue,
			FilterValueKind.TimeSpan => _timeSpanValue,
			FilterValueKind.DateTimeOffset => _dateTimeOffset,
			FilterValueKind.Enumerable => _valuesList,
			_ => null,
		};

		private FilterValue(bool value) : this(typeof(bool))
		{
			_boolValue = value;
		}

		private FilterValue(bool? value) : this(typeof(bool))
		{
			_boolValue = value;
		}


		private FilterValue(byte value) : this(typeof(byte))
		{
			_integerValue = value;
		}

		private FilterValue(byte? value) : this(typeof(byte))
		{
			_integerValue = value;
		}

		private FilterValue(short value) : this(typeof(short))
		{
			_integerValue = value;
		}

		private FilterValue(short? value) : this(typeof(short))
		{
			_integerValue = value;
		}

		private FilterValue(int value) : this(typeof(int))
		{
			_integerValue = value;
		}

		private FilterValue(int? value) : this(typeof(int))
		{
			_integerValue = value;
		}

		private FilterValue(long value) : this(typeof(long))
		{
			_integerValue = value;
		}

		private FilterValue(long? value) : this(typeof(long))
		{
			_integerValue = value;
		}

		private FilterValue(float value) : this(typeof(float))
		{
			_doubleValue = value;
		}

		private FilterValue(float? value) : this(typeof(float))
		{
			_doubleValue = value;
		}

		private FilterValue(double value) : this(typeof(double))
		{
			_doubleValue = value;
		}

		private FilterValue(decimal value) : this(typeof(decimal))
		{
			_decimalValue = value;
		}

		private FilterValue(decimal? value) : this(typeof(decimal))
		{
			_decimalValue = value;
		}

		private FilterValue(double? value) : this(typeof(double))
		{
			_doubleValue = value;
		}

		private FilterValue(char value) : this(typeof(char))
		{
			_charValue = value;
		}

		private FilterValue(char? value) : this(typeof(char))
		{
			_charValue = value;
		}

		private FilterValue(string value) : this(typeof(string))
		{
			_stringValue = value;
		}

		private FilterValue(Guid value) : this(typeof(Guid))
		{
			_guidValue = value;
		}

		private FilterValue(Guid? value) : this(typeof(Guid))
		{
			_guidValue = value;
		}

		private FilterValue(DateTime value) : this(typeof(DateTime))
		{
			_dateTimeValue = value;
		}

		private FilterValue(DateTime? value) : this(typeof(DateTime))
		{
			_dateTimeValue = value;
		}

		private FilterValue(DateOnly value) : this(typeof(DateOnly))
		{
			_dateOnlyValue = value;
		}

		private FilterValue(DateOnly? value) : this(typeof(DateOnly))
		{
			_dateOnlyValue = value;
		}

		private FilterValue(TimeOnly value) : this(typeof(TimeOnly))
		{
			_timeOnlyValue = value;
		}

		private FilterValue(TimeOnly? value) : this(typeof(TimeOnly))
		{
			_timeOnlyValue = value;
		}

		private FilterValue(TimeSpan value) : this(typeof(TimeSpan))
		{
			_timeSpanValue = value;
		}

		private FilterValue(TimeSpan? value) : this(typeof(TimeSpan))
		{
			_timeSpanValue = value;
		}

		private FilterValue(DateTimeOffset value) : this(typeof(DateTimeOffset))
		{
			_dateTimeOffset = value;
		}

		private FilterValue(DateTimeOffset? value) : this(typeof(DateTimeOffset))
		{
			_dateTimeOffset = value;
		}

		private FilterValue(List<object> value) : this(typeof(List<>))
		{
			_valuesList = value;
		}

		private static readonly Func<Type, FilterValueKind, string> castExceptionMessage = (Type t1, FilterValueKind kind)
			=> $"A {kind} filter value can't be read as {t1.Name}.";

		/// <summary>Wraps a <c>bool</c> value.</summary>
		public static implicit operator FilterValue(bool value) => new(value);
		/// <summary>Wraps a <c>bool?</c> value.</summary>
		public static implicit operator FilterValue(bool? value) => new(value);
		/// <summary>Reads the value as <c>bool</c>. A missing value reads as the default.</summary>
		/// <exception cref="InvalidCastException">The value is of another kind.</exception>
		public static explicit operator bool(FilterValue d) => d._kind is FilterValueKind.Bool
			? d._boolValue ?? default
			: throw new InvalidCastException(castExceptionMessage(typeof(bool), d._kind));
		/// <summary>Reads the value as <c>bool?</c>.</summary>
		/// <exception cref="InvalidCastException">The value is of another kind.</exception>
		public static explicit operator bool?(FilterValue d) => d._kind is FilterValueKind.Bool
			? d._boolValue
			: throw new InvalidCastException(castExceptionMessage(typeof(bool), d._kind));

		/// <summary>Wraps a <c>byte</c> value.</summary>
		public static implicit operator FilterValue(byte value) => new(value);
		/// <summary>Wraps a <c>byte?</c> value.</summary>
		public static implicit operator FilterValue(byte? value) => new(value);
		/// <summary>Reads the value as <c>byte</c>. A missing value reads as the default.</summary>
		/// <exception cref="InvalidCastException">The value is of another kind.</exception>
		/// <exception cref="OverflowException">The value is outside the range of <c>byte</c>.</exception>
		public static explicit operator byte(FilterValue d) => d._kind is FilterValueKind.Integer
			? d._integerValue == null ? (byte)0 : checked((byte)d._integerValue.Value)
			: throw new InvalidCastException(castExceptionMessage(typeof(byte), d._kind));
		/// <summary>Reads the value as <c>byte?</c>.</summary>
		/// <exception cref="InvalidCastException">The value is of another kind.</exception>
		/// <exception cref="OverflowException">The value is outside the range of <c>byte</c>.</exception>
		public static explicit operator byte?(FilterValue d) => d._kind is FilterValueKind.Integer
			? checked((byte?)d._integerValue)
			: throw new InvalidCastException(castExceptionMessage(typeof(byte), d._kind));

		/// <summary>Wraps a <c>short</c> value.</summary>
		public static implicit operator FilterValue(short value) => new(value);
		/// <summary>Wraps a <c>short?</c> value.</summary>
		public static implicit operator FilterValue(short? value) => new(value);
		/// <summary>Reads the value as <c>short</c>. A missing value reads as the default.</summary>
		/// <exception cref="InvalidCastException">The value is of another kind.</exception>
		/// <exception cref="OverflowException">The value is outside the range of <c>short</c>.</exception>
		public static explicit operator short(FilterValue d) => d._kind is FilterValueKind.Integer
			? d._integerValue == null ? default : checked((short)d._integerValue.Value)
			: throw new InvalidCastException(castExceptionMessage(typeof(short), d._kind));
		/// <summary>Reads the value as <c>short?</c>.</summary>
		/// <exception cref="InvalidCastException">The value is of another kind.</exception>
		/// <exception cref="OverflowException">The value is outside the range of <c>short</c>.</exception>
		public static explicit operator short?(FilterValue d) => d._kind is FilterValueKind.Integer
			? checked((short?)d._integerValue)
			: throw new InvalidCastException(castExceptionMessage(typeof(short), d._kind));

		/// <summary>Wraps a <c>int</c> value.</summary>
		public static implicit operator FilterValue(int value) => new(value);
		/// <summary>Wraps a <c>int?</c> value.</summary>
		public static implicit operator FilterValue(int? value) => new(value);
		/// <summary>Reads the value as <c>int</c>. A missing value reads as the default.</summary>
		/// <exception cref="InvalidCastException">The value is of another kind.</exception>
		/// <exception cref="OverflowException">The value is outside the range of <c>int</c>.</exception>
		public static explicit operator int(FilterValue d) => d._kind is FilterValueKind.Integer
			? d._integerValue == null ? default : checked((int)d._integerValue.Value)
			: throw new InvalidCastException(castExceptionMessage(typeof(int), d._kind));
		/// <summary>Reads the value as <c>int?</c>.</summary>
		/// <exception cref="InvalidCastException">The value is of another kind.</exception>
		/// <exception cref="OverflowException">The value is outside the range of <c>int</c>.</exception>
		public static explicit operator int?(FilterValue d) => d._kind is FilterValueKind.Integer
			? checked((int?)d._integerValue)
			: throw new InvalidCastException(castExceptionMessage(typeof(int), d._kind));

		/// <summary>Wraps a <c>long?</c> value.</summary>
		public static implicit operator FilterValue(long? value) => new(value);
		/// <summary>Wraps a <c>long</c> value.</summary>
		public static implicit operator FilterValue(long value) => new(value);
		/// <summary>Reads the value as <c>long</c>. A missing value reads as the default.</summary>
		/// <exception cref="InvalidCastException">The value is of another kind.</exception>
		public static explicit operator long(FilterValue d) => d._kind is FilterValueKind.Integer
			? d._integerValue ?? default
			: throw new InvalidCastException(castExceptionMessage(typeof(long), d._kind));
		/// <summary>Reads the value as <c>long?</c>.</summary>
		/// <exception cref="InvalidCastException">The value is of another kind.</exception>
		public static explicit operator long?(FilterValue d) => d._kind is FilterValueKind.Integer
			? d._integerValue
			: throw new InvalidCastException(castExceptionMessage(typeof(long), d._kind));

		/// <summary>Wraps a <c>string</c> value.</summary>
		public static implicit operator FilterValue(string value) => new(value);
		/// <summary>Reads the value as <c>string</c>.</summary>
		/// <exception cref="InvalidCastException">The value is of another kind.</exception>
		public static explicit operator string(FilterValue d) => d._kind is FilterValueKind.String
			? d._stringValue!
			: throw new InvalidCastException(castExceptionMessage(typeof(string), d._kind));

		/// <summary>Wraps a <c>float</c> value.</summary>
		public static implicit operator FilterValue(float value) => new(value);
		/// <summary>Wraps a <c>float?</c> value.</summary>
		public static implicit operator FilterValue(float? value) => new(value);
		/// <summary>Reads the value as <c>float</c>. A missing value reads as the default.</summary>
		/// <exception cref="InvalidCastException">The value isn't of kind <see cref="FilterValueKind.Double"/> or <see cref="FilterValueKind.Decimal"/>.</exception>
		/// <exception cref="OverflowException">The value is outside the range of <c>float</c>.</exception>
		public static explicit operator float(FilterValue d) => d._kind is FilterValueKind.Double or FilterValueKind.Decimal
			? ToFloatChecked(d._doubleValue) ?? (float?)d._decimalValue ?? default
			: throw new InvalidCastException(castExceptionMessage(typeof(float), d._kind));
		/// <summary>Reads the value as <c>float?</c>.</summary>
		/// <exception cref="InvalidCastException">The value isn't of kind <see cref="FilterValueKind.Double"/> or <see cref="FilterValueKind.Decimal"/>.</exception>
		/// <exception cref="OverflowException">The value is outside the range of <c>float</c>.</exception>
		public static explicit operator float?(FilterValue d) => d._kind is FilterValueKind.Double or FilterValueKind.Decimal
			? ToFloatChecked(d._doubleValue) ?? (float?)d._decimalValue
			: throw new InvalidCastException(castExceptionMessage(typeof(float), d._kind));

		// double → float doesn't throw when the value is too big, it gives infinity, so check by hand.
		private static float? ToFloatChecked(double? value) =>
			value is double v && double.IsFinite(v) && float.IsInfinity((float)v)
				? throw new OverflowException($"The value {v} is outside the range of {typeof(float)}.")
				: (float?)value;

		/// <summary>Wraps a <c>double</c> value.</summary>
		public static implicit operator FilterValue(double value) => new(value);
		/// <summary>Wraps a <c>double?</c> value.</summary>
		public static implicit operator FilterValue(double? value) => new(value);
		/// <summary>Reads the value as <c>double</c>. A missing value reads as the default.</summary>
		/// <exception cref="InvalidCastException">The value isn't of kind <see cref="FilterValueKind.Double"/> or <see cref="FilterValueKind.Decimal"/>.</exception>
		public static explicit operator double(FilterValue d) => d._kind is FilterValueKind.Double or FilterValueKind.Decimal
			? d._doubleValue ?? (double?)d._decimalValue ?? default
			: throw new InvalidCastException(castExceptionMessage(typeof(double), d._kind));
		/// <summary>Reads the value as <c>double?</c>.</summary>
		/// <exception cref="InvalidCastException">The value isn't of kind <see cref="FilterValueKind.Double"/> or <see cref="FilterValueKind.Decimal"/>.</exception>
		public static explicit operator double?(FilterValue d) => d._kind is FilterValueKind.Double or FilterValueKind.Decimal
			? d._doubleValue ?? (double?)d._decimalValue
			: throw new InvalidCastException(castExceptionMessage(typeof(double), d._kind));

		/// <summary>Wraps a <c>decimal</c> value.</summary>
		public static implicit operator FilterValue(decimal value) => new(value);
		/// <summary>Wraps a <c>decimal?</c> value.</summary>
		public static implicit operator FilterValue(decimal? value) => new(value);
		/// <summary>Reads the value as <c>decimal</c>. A missing value reads as the default.</summary>
		/// <exception cref="InvalidCastException">The value isn't of kind <see cref="FilterValueKind.Double"/> or <see cref="FilterValueKind.Decimal"/>.</exception>
		/// <exception cref="OverflowException">The value is outside the range of <c>decimal</c>.</exception>
		public static explicit operator decimal(FilterValue d) => d._kind is FilterValueKind.Decimal or FilterValueKind.Double
			? d._decimalValue ?? (decimal?)d._doubleValue ?? default
			: throw new InvalidCastException(castExceptionMessage(typeof(decimal), d._kind));
		/// <summary>Reads the value as <c>decimal?</c>.</summary>
		/// <exception cref="InvalidCastException">The value isn't of kind <see cref="FilterValueKind.Double"/> or <see cref="FilterValueKind.Decimal"/>.</exception>
		/// <exception cref="OverflowException">The value is outside the range of <c>decimal</c>.</exception>
		public static explicit operator decimal?(FilterValue d) => d._kind is FilterValueKind.Decimal or FilterValueKind.Double
			? d._decimalValue ?? (decimal?)d._doubleValue
			: throw new InvalidCastException(castExceptionMessage(typeof(decimal), d._kind));

		/// <summary>Wraps a <c>char</c> value.</summary>
		public static implicit operator FilterValue(char value) => new(value);
		/// <summary>Wraps a <c>char?</c> value.</summary>
		public static implicit operator FilterValue(char? value) => new(value);
		/// <summary>Reads the value as <c>char</c>. A missing value reads as the default.</summary>
		/// <exception cref="InvalidCastException">The value is of another kind.</exception>
		public static explicit operator char(FilterValue d) => d._kind is FilterValueKind.Char
			? d._charValue ?? default
			: throw new InvalidCastException(castExceptionMessage(typeof(char), d._kind));
		/// <summary>Reads the value as <c>char?</c>.</summary>
		/// <exception cref="InvalidCastException">The value is of another kind.</exception>
		public static explicit operator char?(FilterValue d) => d._kind is FilterValueKind.Char
			? d._charValue
			: throw new InvalidCastException(castExceptionMessage(typeof(char), d._kind));

		/// <summary>Wraps a <c>Guid</c> value.</summary>
		public static implicit operator FilterValue(Guid value) => new(value);
		/// <summary>Wraps a <c>Guid?</c> value.</summary>
		public static implicit operator FilterValue(Guid? value) => new(value);
		/// <summary>Reads the value as <c>Guid</c>. A missing value reads as the default.</summary>
		/// <exception cref="InvalidCastException">The value is neither a <c>Guid</c> nor a string.</exception>
		/// <exception cref="FormatException">The value is a string that isn't a valid <c>Guid</c>.</exception>
		public static explicit operator Guid(FilterValue d) => d._kind switch
		{
			FilterValueKind.Guid => d._guidValue ?? default,
			FilterValueKind.String => d._stringValue is null ? default : ParseGuid(d._stringValue),
			_ => throw new InvalidCastException(castExceptionMessage(typeof(Guid), d._kind)),
		};
		/// <summary>Reads the value as <c>Guid?</c>.</summary>
		/// <exception cref="InvalidCastException">The value is neither a <c>Guid</c> nor a string.</exception>
		/// <exception cref="FormatException">The value is a string that isn't a valid <c>Guid</c>.</exception>
		public static explicit operator Guid?(FilterValue d) => d._kind switch
		{
			FilterValueKind.Guid => d._guidValue,
			FilterValueKind.String => d._stringValue is null ? null : ParseGuid(d._stringValue),
			_ => throw new InvalidCastException(castExceptionMessage(typeof(Guid), d._kind)),
		};

		private static Guid ParseGuid(string value) => Guid.TryParse(value, out var guid)
			? guid
			: throw new FormatException($"'{value}' is not a valid Guid.");

		/// <summary>Wraps a <c>DateTime</c> value.</summary>
		public static implicit operator FilterValue(DateTime value) => new(value);
		/// <summary>Wraps a <c>DateTime?</c> value.</summary>
		public static implicit operator FilterValue(DateTime? value) => new(value);
		/// <summary>Reads the value as <c>DateTime</c>. A missing value reads as the default.</summary>
		/// <exception cref="InvalidCastException">The value is of another kind.</exception>
		public static explicit operator DateTime(FilterValue d) => d._kind is FilterValueKind.DateTime
			? d._dateTimeValue ?? default
			: throw new InvalidCastException(castExceptionMessage(typeof(DateTime), d._kind));
		/// <summary>Reads the value as <c>DateTime?</c>.</summary>
		/// <exception cref="InvalidCastException">The value is of another kind.</exception>
		public static explicit operator DateTime?(FilterValue d) => d._kind is FilterValueKind.DateTime
			? d._dateTimeValue
			: throw new InvalidCastException(castExceptionMessage(typeof(DateTime), d._kind));

		/// <summary>Wraps a <c>DateOnly</c> value.</summary>
		public static implicit operator FilterValue(DateOnly value) => new(value);
		/// <summary>Wraps a <c>DateOnly?</c> value.</summary>
		public static implicit operator FilterValue(DateOnly? value) => new(value);
		/// <summary>Reads the value as <c>DateOnly</c>. A missing value reads as the default.</summary>
		/// <exception cref="InvalidCastException">The value is of another kind.</exception>
		public static explicit operator DateOnly(FilterValue d) => d._kind is FilterValueKind.DateOnly
			? d._dateOnlyValue ?? default
			: throw new InvalidCastException(castExceptionMessage(typeof(DateOnly), d._kind));
		/// <summary>Reads the value as <c>DateOnly?</c>.</summary>
		/// <exception cref="InvalidCastException">The value is of another kind.</exception>
		public static explicit operator DateOnly?(FilterValue d) => d._kind is FilterValueKind.DateOnly
			? d._dateOnlyValue
			: throw new InvalidCastException(castExceptionMessage(typeof(DateOnly), d._kind));

		/// <summary>Wraps a <c>TimeOnly</c> value.</summary>
		public static implicit operator FilterValue(TimeOnly value) => new(value);
		/// <summary>Wraps a <c>TimeOnly?</c> value.</summary>
		public static implicit operator FilterValue(TimeOnly? value) => new(value);
		/// <summary>Reads the value as <c>TimeOnly</c>. A missing value reads as the default.</summary>
		/// <exception cref="InvalidCastException">The value is of another kind.</exception>
		public static explicit operator TimeOnly(FilterValue d) => d._kind is FilterValueKind.TimeOnly
			? d._timeOnlyValue ?? default
			: throw new InvalidCastException(castExceptionMessage(typeof(TimeOnly), d._kind));
		/// <summary>Reads the value as <c>TimeOnly?</c>.</summary>
		/// <exception cref="InvalidCastException">The value is of another kind.</exception>
		public static explicit operator TimeOnly?(FilterValue d) => d._kind is FilterValueKind.TimeOnly
			? d._timeOnlyValue
			: throw new InvalidCastException(castExceptionMessage(typeof(TimeOnly), d._kind));

		/// <summary>Wraps a <c>TimeSpan</c> value.</summary>
		public static implicit operator FilterValue(TimeSpan value) => new(value);
		/// <summary>Wraps a <c>TimeSpan?</c> value.</summary>
		public static implicit operator FilterValue(TimeSpan? value) => new(value);
		/// <summary>Reads the value as <c>TimeSpan</c>. A missing value reads as the default.</summary>
		/// <exception cref="InvalidCastException">The value is of another kind.</exception>
		public static explicit operator TimeSpan(FilterValue d) => d._kind is FilterValueKind.TimeSpan
			? d._timeSpanValue ?? default
			: throw new InvalidCastException(castExceptionMessage(typeof(TimeSpan), d._kind));
		/// <summary>Reads the value as <c>TimeSpan?</c>.</summary>
		/// <exception cref="InvalidCastException">The value is of another kind.</exception>
		public static explicit operator TimeSpan?(FilterValue d) => d._kind is FilterValueKind.TimeSpan
			? d._timeSpanValue
			: throw new InvalidCastException(castExceptionMessage(typeof(TimeSpan), d._kind));

		/// <summary>Wraps a <c>DateTimeOffset</c> value.</summary>
		public static implicit operator FilterValue(DateTimeOffset value) => new(value);
		/// <summary>Wraps a <c>DateTimeOffset?</c> value.</summary>
		public static implicit operator FilterValue(DateTimeOffset? value) => new(value);
		/// <summary>Reads the value as <c>DateTimeOffset</c>. A missing value reads as the default.</summary>
		/// <exception cref="InvalidCastException">The value is of another kind.</exception>
		public static explicit operator DateTimeOffset(FilterValue d) => d._kind is FilterValueKind.DateTimeOffset
			? d._dateTimeOffset ?? default
			: throw new InvalidCastException(castExceptionMessage(typeof(DateTimeOffset), d._kind));
		/// <summary>Reads the value as <c>DateTimeOffset?</c>.</summary>
		/// <exception cref="InvalidCastException">The value is of another kind.</exception>
		public static explicit operator DateTimeOffset?(FilterValue d) => d._kind is FilterValueKind.DateTimeOffset
			? d._dateTimeOffset
			: throw new InvalidCastException(castExceptionMessage(typeof(DateTimeOffset), d._kind));

		/// <summary>Wraps a list of values, for <c>In</c>, <c>NotIn</c> and the collection conditions.</summary>
		public static implicit operator FilterValue(List<object> value) => new(value);
		/// <summary>Reads the value as <c>List&lt;object&gt;</c>.</summary>
		/// <exception cref="InvalidCastException">The value is of another kind.</exception>
		public static explicit operator List<object>(FilterValue d) => d._kind is FilterValueKind.Enumerable
			? d._valuesList!
			: throw new InvalidCastException(castExceptionMessage(typeof(List<>), d._kind));
	}

}
