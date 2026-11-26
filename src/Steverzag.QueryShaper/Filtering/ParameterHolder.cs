using System.Collections;
using System.Linq.Expressions;

namespace Steverzag.QueryShaper.Filtering
{
	internal sealed class ParameterHolder<T>(T value)
	{
		public T Value { get; } = value;
	}

	internal static class ParameterHolder
	{
		private static readonly HashSet<Type> InlinedListElementTypes = [typeof(TimeOnly), typeof(TimeSpan), typeof(DateTimeOffset)];

		public static Expression Parameter(object? value, Type type)
		{
			var holder = Activator.CreateInstance(typeof(ParameterHolder<>).MakeGenericType(type), [value])!;
			return Expression.Property(Expression.Constant(holder), nameof(ParameterHolder<object>.Value));
		}

		public static Expression List(IList list)
		{
			var listType = list.GetType();
			var elementType = listType.GetGenericArguments()[0];
			var underlyingType = Nullable.GetUnderlyingType(elementType) ?? elementType;

			return InlinedListElementTypes.Contains(underlyingType)
				? Expression.Constant(list, listType)
				: Parameter(list, listType);
		}
	}
}
