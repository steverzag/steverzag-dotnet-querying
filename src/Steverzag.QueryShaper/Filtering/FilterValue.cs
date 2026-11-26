using System.Collections;
using System.Linq.Expressions;

namespace Steverzag.QueryShaper.Filtering
{
	internal sealed class FilterValue<T>(T value)
	{
		public T Value { get; } = value;
	}

	internal static class FilterValue
	{
		private static readonly HashSet<Type> InlinedListElementTypes = [typeof(TimeOnly), typeof(TimeSpan), typeof(DateTimeOffset)];

		public static Expression Parameter(object? value, Type type)
		{
			var holder = Activator.CreateInstance(typeof(FilterValue<>).MakeGenericType(type), [value])!;
			return Expression.Property(Expression.Constant(holder), nameof(FilterValue<object>.Value));
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
