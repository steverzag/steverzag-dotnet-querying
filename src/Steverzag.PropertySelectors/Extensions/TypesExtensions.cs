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
	{
		var differentlyCased = PublicProperties(type)
			.Select(property => property.Name)
			.FirstOrDefault(name => string.Equals(name, propertyName, StringComparison.OrdinalIgnoreCase));
		var hint = differentlyCased is null ? string.Empty : $" Property names are case-sensitive: '{differentlyCased}'.";

		return new($"Property '{propertyName}' was not found on type '{type.Name}'.{hint}", propertyPath, offset, propertyName, innerException);
	}

	public static Type GetPropertyType(this Type rootType, PropertySelector propertySelector)
	{
		ArgumentNullException.ThrowIfNull(rootType, nameof(rootType));
		ArgumentNullException.ThrowIfNull(propertySelector, nameof(propertySelector));

		var propertyPath = propertySelector?.PlainValue;
		var source = MemberFilter.TakePipedSource();

		if (string.IsNullOrWhiteSpace(propertyPath))
			return rootType;

		Type currentType = rootType;

		var projected = false;
		var segmentOffset = 0;
		var lifted = false;
		foreach (var segment in propertyPath.Split('.'))
		{
			var segmentStart = segmentOffset;
			segmentOffset += segment.Length + 1;
			var propertyName = UnescapePropertyName(segment, propertyPath, segmentStart);
			var offset = segmentStart + segment.Length - propertyName.Length;

			var property = offset == 0 ? FindProperty(currentType, propertyName) : null;

			if (property is null && ExpressionUtils.TryGetEnumerableElementType(currentType, out var itemType) && itemType is not null)
			{
				projected = true;
				currentType = itemType;
			}

			property ??= FindProperty(currentType, propertyName);
			var owner = currentType;

			if (property is null && Nullable.GetUnderlyingType(currentType) is { } underlying)
			{
				property = FindProperty(underlying, propertyName);
				lifted |= property is not null;
				owner = underlying;
			}

			if (property is null)
			{
				throw PropertyNotFound(propertyName, Nullable.GetUnderlyingType(currentType) ?? currentType, propertyPath, offset);
			}

			MemberFilter.Check(owner, property, source, propertyPath, offset);
			source = (property, owner);

			currentType = property.PropertyType;
		}

		if (source is { } last)
			MemberFilter.Remember(propertySelector!, last.Property, last.Type);

		if (lifted)
			currentType = ExpressionUtils.LiftToNullable(currentType);

		if (!projected)
			return currentType;

		if (ExpressionUtils.TryGetEnumerableElementType(currentType, out var finalItemType) && finalItemType is not null)
			currentType = finalItemType;

		return typeof(IEnumerable<>).MakeGenericType(currentType);
	}

	internal static string UnescapePropertyName(string segment, string propertyPath, int offset)
	{
		if (!segment.StartsWith(SelectorDefaults.PropertyNameEscape))
			return segment;

		var name = segment[1..];
		if (name.Length == 0 || name[0] == SelectorDefaults.PropertyNameEscape)
			throw new SelectorParseException($"'{SelectorDefaults.PropertyNameEscape}' must be followed by a property name.", propertyPath, offset, SelectorDefaults.PropertyNameEscape.ToString());

		return name;
	}

	internal static PropertyInfo? FindProperty(Type type, string propertyName)
	{
		foreach (var declaring in TypeHierarchy(type))
		{
			foreach (var property in declaring.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
			{
				if (property.Name == propertyName && property.GetIndexParameters().Length == 0)
					return property;
			}
		}

		return null;
	}

	private static IEnumerable<PropertyInfo> PublicProperties(Type type)
		=> TypeHierarchy(type).SelectMany(declaring => declaring.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));

	private static IEnumerable<Type> TypeHierarchy(Type type)
	{
		if (type.IsInterface)
		{
			yield return type;
			foreach (var inherited in type.GetInterfaces())
				yield return inherited;
			yield break;
		}

		for (Type? current = type; current is not null; current = current.BaseType)
			yield return current;
	}
}
