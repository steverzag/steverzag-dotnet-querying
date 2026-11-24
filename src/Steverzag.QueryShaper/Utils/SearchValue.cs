using System.Runtime.CompilerServices;

namespace Steverzag.QueryShaper.Utils
{
	public readonly struct ValueResult2
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

		private readonly ResultValueKind _kind;

		private ValueResult2(Type type)
		{
			_kind = type switch
			{
				Type t when t == typeof(bool) => ResultValueKind.Bool,
				Type t when t == typeof(byte) => ResultValueKind.Integer,
				Type t when t == typeof(short) => ResultValueKind.Integer,
				Type t when t == typeof(int) => ResultValueKind.Integer,
				Type t when t == typeof(long) => ResultValueKind.Integer,
				Type t when t == typeof(float) => ResultValueKind.Double,
				Type t when t == typeof(double) => ResultValueKind.Double,
				Type t when t == typeof(decimal) => ResultValueKind.Decimal,
				Type t when t == typeof(char) => ResultValueKind.Char,
				Type t when t == typeof(string) => ResultValueKind.String,
				Type t when t == typeof(Guid) => ResultValueKind.Guid,
				Type t when t == typeof(DateTime) => ResultValueKind.DateTime,
				Type t when t == typeof(DateOnly) => ResultValueKind.DateOnly,
				Type t when t == typeof(TimeOnly) => ResultValueKind.TimeOnly,
				Type t when t == typeof(TimeSpan) => ResultValueKind.TimeSpan,
				Type t when t == typeof(DateTimeOffset) => ResultValueKind.DateTimeOffset,
				Type t when t == typeof(List<>) => ResultValueKind.Enumerable,
				_ => ResultValueKind.Integer
			};
		}

		public ResultValueKind GetKind() => _kind;

		internal object? ToObject() => _kind switch
		{
			ResultValueKind.Bool => _boolValue,
			ResultValueKind.Integer => _integerValue,
			ResultValueKind.Double => _doubleValue,
			ResultValueKind.Decimal => _decimalValue,
			ResultValueKind.Char => _charValue,
			ResultValueKind.String => _stringValue,
			ResultValueKind.Guid => _guidValue,
			ResultValueKind.DateTime => _dateTimeValue,
			ResultValueKind.DateOnly => _dateOnlyValue,
			ResultValueKind.TimeOnly => _timeOnlyValue,
			ResultValueKind.TimeSpan => _timeSpanValue,
			ResultValueKind.DateTimeOffset => _dateTimeOffset,
			ResultValueKind.Enumerable => _valuesList,
			_ => null,
		};

		private ValueResult2(bool value) : this(typeof(bool))
		{
			_boolValue = value;
		}

		private ValueResult2(bool? value) : this(typeof(bool))
		{
			_boolValue = value;
		}


		private ValueResult2(byte value) : this(typeof(byte))
		{
			_integerValue = value;
		}

		private ValueResult2(byte? value) : this(typeof(byte))
		{
			_integerValue = value;
		}

		private ValueResult2(short value) : this(typeof(short))
		{
			_integerValue = value;
		}

		private ValueResult2(short? value) : this(typeof(short))
		{
			_integerValue = value;
		}

		private ValueResult2(int value) : this(typeof(int))
		{
			_integerValue = value;
		}

		private ValueResult2(int? value) : this(typeof(int))
		{
			_integerValue = value;
		}

		private ValueResult2(long value) : this(typeof(long))
		{
			_integerValue = value;
		}

		private ValueResult2(long? value) : this(typeof(long))
		{
			_integerValue = value;
		}

		private ValueResult2(float value) : this(typeof(float))
		{
			_doubleValue = value;
		}

		private ValueResult2(float? value) : this(typeof(float))
		{
			_doubleValue = value;
		}

		private ValueResult2(double value) : this(typeof(double))
		{
			_doubleValue = value;
		}

		private ValueResult2(decimal value) : this(typeof(decimal))
		{
			_decimalValue = value;
		}

		private ValueResult2(decimal? value) : this(typeof(decimal))
		{
			_decimalValue = value;
		}

		private ValueResult2(double? value) : this(typeof(double))
		{
			_doubleValue = value;
		}

		private ValueResult2(char value) : this(typeof(char))
		{
			_charValue = value;
		}

		private ValueResult2(char? value) : this(typeof(char))
		{
			_charValue = value;
		}

		private ValueResult2(string value) : this(typeof(string))
		{
			_stringValue = value;
		}

		private ValueResult2(Guid value) : this(typeof(Guid))
		{
			_guidValue = value;
		}

		private ValueResult2(Guid? value) : this(typeof(Guid))
		{
			_guidValue = value;
		}

		private ValueResult2(DateTime value) : this(typeof(DateTime))
		{
			_dateTimeValue = value;
		}

		private ValueResult2(DateTime? value) : this(typeof(DateTime))
		{
			_dateTimeValue = value;
		}

		private ValueResult2(DateOnly value) : this(typeof(DateOnly))
		{
			_dateOnlyValue = value;
		}

		private ValueResult2(DateOnly? value) : this(typeof(DateOnly))
		{
			_dateOnlyValue = value;
		}

		private ValueResult2(TimeOnly value) : this(typeof(TimeOnly))
		{
			_timeOnlyValue = value;
		}

		private ValueResult2(TimeOnly? value) : this(typeof(TimeOnly))
		{
			_timeOnlyValue = value;
		}

		private ValueResult2(TimeSpan value) : this(typeof(TimeSpan))
		{
			_timeSpanValue = value;
		}

		private ValueResult2(TimeSpan? value) : this(typeof(TimeSpan))
		{
			_timeSpanValue = value;
		}

		private ValueResult2(DateTimeOffset value) : this(typeof(DateTimeOffset))
		{
			_dateTimeOffset = value;
		}

		private ValueResult2(DateTimeOffset? value) : this(typeof(DateTimeOffset))
		{
			_dateTimeOffset = value;
		}

		private ValueResult2(List<object> value) : this(typeof(List<>))
		{
			_valuesList = value;
		}

		private static readonly Func<Type, ResultValueKind, string> castExceptionMessage = (Type t1, ResultValueKind kind)
			=> $"Invalid Assingnment operation, trying to assign to type {t1} when value is of type {kind}";

		public static implicit operator ValueResult2(bool value) => new(value);
		public static implicit operator ValueResult2(bool? value) => new(value);
		public static explicit operator bool(ValueResult2 d) => d._kind is ResultValueKind.Bool
			? d._boolValue ?? default
			: throw new InvalidCastException(castExceptionMessage(typeof(bool), d._kind));
		public static explicit operator bool?(ValueResult2 d) => d._kind is ResultValueKind.Bool
			? d._boolValue
			: throw new InvalidCastException(castExceptionMessage(typeof(bool), d._kind));

		public static implicit operator ValueResult2(byte value) => new(value);
		public static implicit operator ValueResult2(byte? value) => new(value);
		public static explicit operator byte(ValueResult2 d) => d._kind is ResultValueKind.Integer
			? d._integerValue == null ? (byte)0 : checked((byte)d._integerValue.Value)
			: throw new InvalidCastException(castExceptionMessage(typeof(byte), d._kind));
		public static explicit operator byte?(ValueResult2 d) => d._kind is ResultValueKind.Integer
			? checked((byte?)d._integerValue)
			: throw new InvalidCastException(castExceptionMessage(typeof(byte), d._kind));

		public static implicit operator ValueResult2(short value) => new(value);
		public static implicit operator ValueResult2(short? value) => new(value);
		public static explicit operator short(ValueResult2 d) => d._kind is ResultValueKind.Integer
			? d._integerValue == null ? default : checked((short)d._integerValue.Value)
			: throw new InvalidCastException(castExceptionMessage(typeof(short), d._kind));
		public static explicit operator short?(ValueResult2 d) => d._kind is ResultValueKind.Integer
			? checked((short?)d._integerValue)
			: throw new InvalidCastException(castExceptionMessage(typeof(short), d._kind));

		public static implicit operator ValueResult2(int value) => new(value);
		public static implicit operator ValueResult2(int? value) => new(value);
		public static explicit operator int(ValueResult2 d) => d._kind is ResultValueKind.Integer
			? d._integerValue == null ? default : checked((int)d._integerValue.Value)
			: throw new InvalidCastException(castExceptionMessage(typeof(int), d._kind));
		public static explicit operator int?(ValueResult2 d) => d._kind is ResultValueKind.Integer
			? checked((int?)d._integerValue)
			: throw new InvalidCastException(castExceptionMessage(typeof(int), d._kind));

		public static implicit operator ValueResult2(long? value) => new(value);
		public static implicit operator ValueResult2(long value) => new(value);
		public static explicit operator long(ValueResult2 d) => d._kind is ResultValueKind.Integer
			? d._integerValue ?? default
			: throw new InvalidCastException(castExceptionMessage(typeof(long), d._kind));
		public static explicit operator long?(ValueResult2 d) => d._kind is ResultValueKind.Integer
			? d._integerValue
			: throw new InvalidCastException(castExceptionMessage(typeof(long), d._kind));

		public static implicit operator ValueResult2(string value) => new(value);
		public static explicit operator string(ValueResult2 d) => d._kind is ResultValueKind.String
			? d._stringValue!
			: throw new InvalidCastException(castExceptionMessage(typeof(string), d._kind));

		public static implicit operator ValueResult2(float value) => new(value);
		public static implicit operator ValueResult2(float? value) => new(value);
		public static explicit operator float(ValueResult2 d) => d._kind is ResultValueKind.Double or ResultValueKind.Decimal
			? ToFloatChecked(d._doubleValue) ?? (float?)d._decimalValue ?? default
			: throw new InvalidCastException(castExceptionMessage(typeof(float), d._kind));
		public static explicit operator float?(ValueResult2 d) => d._kind is ResultValueKind.Double or ResultValueKind.Decimal
			? ToFloatChecked(d._doubleValue) ?? (float?)d._decimalValue
			: throw new InvalidCastException(castExceptionMessage(typeof(float), d._kind));

		// double → float doesn't throw when the value is too big, it gives infinity, so check by hand.
		private static float? ToFloatChecked(double? value) =>
			value is double v && double.IsFinite(v) && float.IsInfinity((float)v)
				? throw new OverflowException($"The value {v} is outside the range of {typeof(float)}.")
				: (float?)value;

		public static implicit operator ValueResult2(double value) => new(value);
		public static implicit operator ValueResult2(double? value) => new(value);
		public static explicit operator double(ValueResult2 d) => d._kind is ResultValueKind.Double or ResultValueKind.Decimal
			? d._doubleValue ?? (double?)d._decimalValue ?? default
			: throw new InvalidCastException(castExceptionMessage(typeof(double), d._kind));
		public static explicit operator double?(ValueResult2 d) => d._kind is ResultValueKind.Double or ResultValueKind.Decimal
			? d._doubleValue ?? (double?)d._decimalValue
			: throw new InvalidCastException(castExceptionMessage(typeof(double), d._kind));

		public static implicit operator ValueResult2(decimal value) => new(value);
		public static implicit operator ValueResult2(decimal? value) => new(value);
		public static explicit operator decimal(ValueResult2 d) => d._kind is ResultValueKind.Decimal or ResultValueKind.Double
			? d._decimalValue ?? (decimal?)d._doubleValue ?? default
			: throw new InvalidCastException(castExceptionMessage(typeof(decimal), d._kind));
		public static explicit operator decimal?(ValueResult2 d) => d._kind is ResultValueKind.Decimal or ResultValueKind.Double
			? d._decimalValue ?? (decimal?)d._doubleValue
			: throw new InvalidCastException(castExceptionMessage(typeof(decimal), d._kind));

		public static implicit operator ValueResult2(char value) => new(value);
		public static implicit operator ValueResult2(char? value) => new(value);
		public static explicit operator char(ValueResult2 d) => d._kind is ResultValueKind.Char
			? d._charValue ?? default
			: throw new InvalidCastException(castExceptionMessage(typeof(char), d._kind));
		public static explicit operator char?(ValueResult2 d) => d._kind is ResultValueKind.Char
			? d._charValue
			: throw new InvalidCastException(castExceptionMessage(typeof(char), d._kind));

		public static implicit operator ValueResult2(Guid value) => new(value);
		public static implicit operator ValueResult2(Guid? value) => new(value);
		public static explicit operator Guid(ValueResult2 d) => d._kind is ResultValueKind.Guid or ResultValueKind.String
			? d._guidValue ?? (Guid.TryParse(d._stringValue, out var guid) ? guid : default)
			: throw new InvalidCastException(castExceptionMessage(typeof(Guid), d._kind));
		public static explicit operator Guid?(ValueResult2 d) => d._kind is ResultValueKind.Guid or ResultValueKind.String
			? d._guidValue ?? (Guid.TryParse(d._stringValue, out var guid) ? guid : null)
			: throw new InvalidCastException(castExceptionMessage(typeof(Guid), d._kind));

		public static implicit operator ValueResult2(DateTime value) => new(value);
		public static implicit operator ValueResult2(DateTime? value) => new(value);
		public static explicit operator DateTime(ValueResult2 d) => d._kind is ResultValueKind.DateTime
			? d._dateTimeValue ?? default
			: throw new InvalidCastException(castExceptionMessage(typeof(DateTime), d._kind));
		public static explicit operator DateTime?(ValueResult2 d) => d._kind is ResultValueKind.DateTime
			? d._dateTimeValue
			: throw new InvalidCastException(castExceptionMessage(typeof(DateTime), d._kind));

		public static implicit operator ValueResult2(DateOnly value) => new(value);
		public static implicit operator ValueResult2(DateOnly? value) => new(value);
		public static explicit operator DateOnly(ValueResult2 d) => d._kind is ResultValueKind.DateOnly
			? d._dateOnlyValue ?? default
			: throw new InvalidCastException(castExceptionMessage(typeof(DateOnly), d._kind));
		public static explicit operator DateOnly?(ValueResult2 d) => d._kind is ResultValueKind.DateOnly
			? d._dateOnlyValue
			: throw new InvalidCastException(castExceptionMessage(typeof(DateOnly), d._kind));

		public static implicit operator ValueResult2(TimeOnly value) => new(value);
		public static implicit operator ValueResult2(TimeOnly? value) => new(value);
		public static explicit operator TimeOnly(ValueResult2 d) => d._kind is ResultValueKind.TimeOnly
			? d._timeOnlyValue ?? default
			: throw new InvalidCastException(castExceptionMessage(typeof(TimeOnly), d._kind));
		public static explicit operator TimeOnly?(ValueResult2 d) => d._kind is ResultValueKind.TimeOnly
			? d._timeOnlyValue
			: throw new InvalidCastException(castExceptionMessage(typeof(TimeOnly), d._kind));

		public static implicit operator ValueResult2(TimeSpan value) => new(value);
		public static implicit operator ValueResult2(TimeSpan? value) => new(value);
		public static explicit operator TimeSpan(ValueResult2 d) => d._kind is ResultValueKind.TimeSpan
			? d._timeSpanValue ?? default
			: throw new InvalidCastException(castExceptionMessage(typeof(TimeSpan), d._kind));
		public static explicit operator TimeSpan?(ValueResult2 d) => d._kind is ResultValueKind.TimeSpan
			? d._timeSpanValue
			: throw new InvalidCastException(castExceptionMessage(typeof(TimeSpan), d._kind));

		public static implicit operator ValueResult2(DateTimeOffset value) => new(value);
		public static implicit operator ValueResult2(DateTimeOffset? value) => new(value);
		public static explicit operator DateTimeOffset(ValueResult2 d) => d._kind is ResultValueKind.DateTimeOffset
			? d._dateTimeOffset ?? default
			: throw new InvalidCastException(castExceptionMessage(typeof(DateTimeOffset), d._kind));
		public static explicit operator DateTimeOffset?(ValueResult2 d) => d._kind is ResultValueKind.DateTimeOffset
			? d._dateTimeOffset
			: throw new InvalidCastException(castExceptionMessage(typeof(DateTimeOffset), d._kind));

		public static implicit operator ValueResult2(List<object> value) => new(value);
		public static explicit operator List<object>(ValueResult2 d) => d._kind is ResultValueKind.Enumerable
			? d._valuesList!
			: throw new InvalidCastException(castExceptionMessage(typeof(List<>), d._kind));
	}

}
