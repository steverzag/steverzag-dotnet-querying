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

		// This stores the OUTERMOST collection type only
		// so nested collections behave like SelectMany()
		Type? outerCollectionType = null;

		var segmentOffset = 0;
		var lifted = false;
		foreach (var propertyName in propertyPath.Split('.'))
		{
			var offset = segmentOffset;
			segmentOffset += propertyName.Length + 1;

			if (TryGetCollectionInfo(currentType, out var itemType, out var collectionType))
			{
				// Preserve only the first collection encountered
				if (outerCollectionType is null)
					outerCollectionType = collectionType;

				// Continue traversal using the inner type
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

		// Final unwrap if result itself is still a collection
		if (outerCollectionType is not null && TryGetCollectionInfo(currentType, out var finalItemType, out _))
		{
			currentType = finalItemType;
		}
		// Final unwrap if result itself is still a collection
		//if (TryGetCollectionInfo(currentType, out var finalItemType, out _))
		//{
		//	currentType = finalItemType;
		//}

		// Rebuild only using the OUTERMOST collection type
		if (outerCollectionType is not null)
		{
			currentType = outerCollectionType.MakeGenericType(currentType);
		}

		return currentType;
	}

	private static bool TryGetCollectionInfo(
		Type type,
		[NotNullWhen(true)] out Type? itemType,
		[NotNullWhen(true)] out Type? collectionType)
	{
		itemType = null;
		collectionType = null;

		if (type == typeof(string))
			return false;

		// Direct generic collections
		if (type.IsGenericType &&
			typeof(IEnumerable).IsAssignableFrom(type))
		{
			var args = type.GetGenericArguments();

			if (args.Length == 1)
			{
				itemType = args[0];
				collectionType = type.GetGenericTypeDefinition();
				return true;
			}
		}

		// Interface fallback
		var enumerableInterface = type
			.GetInterfaces()
			.FirstOrDefault(i =>
				i.IsGenericType &&
				i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

		if (enumerableInterface != null)
		{
			itemType = enumerableInterface.GetGenericArguments()[0];

			if (type.IsGenericType)
				collectionType = type.GetGenericTypeDefinition();
			else
				collectionType = typeof(IEnumerable<>);

			return true;
		}

		return false;
	}
}
