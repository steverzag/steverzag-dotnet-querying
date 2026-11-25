using System.Diagnostics.CodeAnalysis;
using Steverzag.PropertySelectors.MetaData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Steverzag.PropertySelectors.Extensions;

public static class TypesExtensions
{
	internal static SelectorParseException PropertyNotFound(string propertyName, Type type, string propertyPath, int offset, Exception? innerException = null)
		=> new($"Property '{propertyName}' was not found on type '{type.Name}'.", propertyPath, offset, propertyName, innerException);

	public static Type GetPropertyType(this Type rootType, PropertySelector propertySelector)
	{
		ArgumentNullException.ThrowIfNull(rootType, nameof(rootType));
		ArgumentNullException.ThrowIfNull(propertySelector, nameof(propertySelector));

		var propertyPath = propertySelector?.PlainValue;

		if (string.IsNullOrWhiteSpace(propertyPath))
			return rootType;

		Type currentType = rootType;

		var projected = false;
		var segmentOffset = 0;
		var lifted = false;
		foreach (var propertyName in propertyPath.Split('.'))
		{
			var offset = segmentOffset;
			segmentOffset += propertyName.Length + 1;

			if (ExpressionUtils.TryGetEnumerableElementType(currentType, out var itemType) && itemType is not null)
			{
				projected = true;
				currentType = itemType;
			}

			var property = currentType.GetProperty(
				propertyName,
				BindingFlags.Public |
				BindingFlags.Instance |
				BindingFlags.IgnoreCase);

			if (property is null && Nullable.GetUnderlyingType(currentType) is { } underlying)
			{
				property = underlying.GetProperty(
					propertyName,
					BindingFlags.Public |
					BindingFlags.Instance |
					BindingFlags.IgnoreCase);
				lifted |= property is not null;
			}

			if (property is null)
			{
				throw PropertyNotFound(propertyName, Nullable.GetUnderlyingType(currentType) ?? currentType, propertyPath, offset);
			}

			currentType = property.PropertyType;
		}

		if (lifted)
			currentType = ExpressionUtils.LiftToNullable(currentType);

		if (!projected)
			return currentType;

		if (ExpressionUtils.TryGetEnumerableElementType(currentType, out var finalItemType) && finalItemType is not null)
			currentType = finalItemType;

		return typeof(IEnumerable<>).MakeGenericType(currentType);
	}
}
