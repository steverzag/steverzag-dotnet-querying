using ExpDataWorker.PropertySelector;
using ExpDataWorker.PropertySelector.AggregationSelectors;
using ExpDataWorker.PropertySelector.ElementSelectors;
using ExpDataWorker.PropertySelector.OrderingSelectors;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace ExpDataWorker.PropertySelector
{
	public class ExpressionUtils
	{
		private static readonly MethodInfo EnumerableSelectDef;
		private static readonly MethodInfo EnumerableSelectManyDef;
		private static readonly ConcurrentDictionary<Type, Type?> EnumerableElementTypeCache = new();
		private static readonly ConcurrentDictionary<Type, (Type? Key, Type? Element)?> EnumerableGroupingTypeCache = new();

		static ExpressionUtils()
		{
			var methods = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static);
			foreach (var m in methods)
			{
				if (!m.IsGenericMethodDefinition) continue;
				if (m.GetParameters().Length != 2) continue;

				if (m.Name == "Select" && EnumerableSelectDef == null)
					EnumerableSelectDef = m;
				else if (m.Name == "SelectMany" && EnumerableSelectManyDef == null)
					EnumerableSelectManyDef = m;

				if (EnumerableSelectDef != null && EnumerableSelectManyDef != null)
					break;
			}
		}

		public static bool TryGetEnumerableElementType(Type type, out Type? elementType)
		{
			if (EnumerableElementTypeCache.TryGetValue(type, out elementType))
				return elementType is not null;

			if (type == typeof(string))
			{
				elementType = null;
				EnumerableElementTypeCache[type] = null;
				return false;
			}

			if (type.IsArray)
			{
				elementType = type.GetElementType();
				EnumerableElementTypeCache[type] = elementType;
				return elementType is not null;
			}

			if (type.IsGenericType)
			{
				var gdef = type.GetGenericTypeDefinition();
				if (gdef == typeof(IEnumerable<>))
				{
					elementType = type.GetGenericArguments()[0];
					EnumerableElementTypeCache[type] = elementType;
					return true;
				}
			}

			var interfaces = type.GetInterfaces();
			for (int i = 0; i < interfaces.Length; i++)
			{
				var it = interfaces[i];
				if (!it.IsGenericType) continue;
				if (it.GetGenericTypeDefinition() == typeof(IEnumerable<>))
				{
					elementType = it.GetGenericArguments()[0];
					EnumerableElementTypeCache[type] = elementType;
					return true;
				}
			}

			elementType = null;
			EnumerableElementTypeCache[type] = null;
			return false;
		}

		public static Expression CreateMemberAccess(Expression target, PropertySelector selector)
		{
			if (selector is null || string.IsNullOrEmpty(selector.PlainValue)) return target;

			var temp = selector;

			Expression memberAccess = target!;
			while (temp is not null)
			{
				memberAccess = temp.PropertyAccess(memberAccess);
				temp = temp.ChildSelector;
			}

			return memberAccess;
		}

		public static Expression CreateMemberAccessRec(Expression target, string[] parts, int index)
		{
			Expression current = target;

			for (int i = index; i < parts.Length; i++)
			{
				var part = parts[i];
				MemberExpression property = null;

				try
				{
					property = Expression.PropertyOrField(current, part);
				}
				catch (Exception ex)
				{
					if (!TryGetGroupingTypes(target.Type, out var _, out var groupElementType) || groupElementType is null)
						throw;

					var param = Expression.Parameter(groupElementType, "y");
					var innerAccess = CreateMemberAccessRec(param, parts, index);

					if (TryGetEnumerableElementType(innerAccess.Type, out var innerElement) && innerElement != null)
					{
						var enumerableOfResultType = typeof(IEnumerable<>).MakeGenericType(innerElement);
						var lambdaType = typeof(Func<,>).MakeGenericType(param.Type, enumerableOfResultType);
						var lambda = Expression.Lambda(lambdaType, innerAccess, param);

						var method = EnumerableSelectManyDef!.MakeGenericMethod(groupElementType, innerElement);
						current = Expression.Call(method, target, lambda);
					}
					else
					{
						var lambda = Expression.Lambda(innerAccess, param);
						var resultType = innerAccess.Type;
						var method = EnumerableSelectDef!.MakeGenericMethod(groupElementType, resultType);

						current = Expression.Call(method, target, lambda);
					}

					break;
				}

				var propertyType = property.Type;

				if (TryGetEnumerableElementType(propertyType, out var elementType) && elementType != null)
				{
					var param = Expression.Parameter(elementType, "y");

					var nextIndex = i + 1;
					if (nextIndex >= parts.Length)
					{
						current = property;
						break;
					}

					var innerAccess = CreateMemberAccessRec(param, parts, nextIndex);

					if (TryGetEnumerableElementType(innerAccess.Type, out var innerElement) && innerElement != null)
					{
						var enumerableOfResultType = typeof(IEnumerable<>).MakeGenericType(innerElement);
						var lambdaType = typeof(Func<,>).MakeGenericType(param.Type, enumerableOfResultType);
						var lambda = Expression.Lambda(lambdaType, innerAccess, param);

						var method = EnumerableSelectManyDef!.MakeGenericMethod(elementType, innerElement);
						current = Expression.Call(method, property, lambda);
					}
					else
					{
						var lambda = Expression.Lambda(innerAccess, param);
						var resultType = innerAccess.Type;
						var method = EnumerableSelectDef!.MakeGenericMethod(elementType, resultType);

						current = Expression.Call(method, property, lambda);
					}

					break;
				}
				else
				{
					current = property;
				}
			}

			return current;
		}

		public static string ToFlatString(Expression exp)
		{
			if (exp is BinaryExpression be && be.NodeType == ExpressionType.OrElse)
			{
				return $"{ToFlatString(be.Left)} || {ToFlatString(be.Right)}";
			}
			return exp.ToString();
		}

		public static bool TryGetGroupingTypes(Type type, out Type? keyType, out Type? elementType)
		{
			// fast cache hit
			if (EnumerableGroupingTypeCache.TryGetValue(type, out var cached))
			{
				if (cached.HasValue)
				{
					keyType = cached.Value.Key;
					elementType = cached.Value.Element;
					return keyType is not null && elementType is not null;
				}

				keyType = null;
				elementType = null;
				return false;
			}

			// Avoid treating string as enumerable/grouping
			if (type == typeof(string))
			{
				keyType = null;
				elementType = null;
				EnumerableGroupingTypeCache[type] = null;
				return false;
			}

			// If the type itself is a generic IGrouping<,>
			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IGrouping<,>))
			{
				var args = type.GetGenericArguments();
				keyType = args[0];
				elementType = args[1];
				EnumerableGroupingTypeCache[type] = (keyType, elementType);
				return true;
			}

			// Inspect interfaces for IGrouping<,>
			var interfaces = type.GetInterfaces();
			for (int i = 0; i < interfaces.Length; i++)
			{
				var it = interfaces[i];
				if (!it.IsGenericType) continue;
				if (it.GetGenericTypeDefinition() == typeof(IGrouping<,>))
				{
					var args = it.GetGenericArguments();
					keyType = args[0];
					elementType = args[1];
					EnumerableGroupingTypeCache[type] = (keyType, elementType);
					return true;
				}
			}

			// Not an IGrouping
			keyType = null;
			elementType = null;
			EnumerableGroupingTypeCache[type] = null;
			return false;
		}
	}
}
