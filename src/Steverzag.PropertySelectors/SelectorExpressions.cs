using Steverzag.PropertySelectors.SelectorImplementations;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Steverzag.PropertySelectors
{
	/// <summary>Builds LINQ expressions from selectors, for callers and for custom functions.</summary>
	public static class SelectorExpressions
	{
		private static readonly ConcurrentDictionary<Type, Type?> EnumerableElementTypeCache = new();
		private static readonly ConcurrentDictionary<Type, (Type? Key, Type? Element)?> EnumerableGroupingTypeCache = new();

		/// <summary>
		/// Gets the element type of a collection type, by the rule selectors use: arrays and anything that implements
		/// <see cref="IEnumerable{T}"/> are collections, and <see cref="string"/> isn't.
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <param name="elementType">The element type, or <see langword="null"/> if the type isn't a collection.</param>
		/// <returns><see langword="true"/> if the type is a collection.</returns>
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

		[ThreadStatic]
		private static Expression? _scopeRoot;

		/// <summary>
		/// Builds the expression for a function argument. A custom function calls it for each argument when it builds its
		/// expression.
		/// </summary>
		/// <remarks>
		/// The placeholder a piped function receives as argument 0 becomes the piped value. Any other argument is resolved
		/// against the scope root, the value the whole selector starts from, not against the piped value.
		/// </remarks>
		/// <param name="target">The function's target, as its expression builder receives it.</param>
		/// <param name="argument">One of the function's arguments.</param>
		/// <returns>The argument's expression.</returns>
		public static Expression ResolveArgument(Expression? target, PropertySelector argument) =>
			argument is PipedValueSelector
				? target!
				: CreateMemberAccess(ArgumentRoot(target), argument);

		internal static Expression? ArgumentRoot(Expression? target)
			=> FindRootParameter(target) ?? _scopeRoot ?? target;

		/// <summary>Builds the expression for a whole selector, applying each part of the chain in turn.</summary>
		/// <param name="target">The expression the selector starts from, usually a lambda parameter of the root type.</param>
		/// <param name="selector">The selector.</param>
		/// <returns>The selector's expression, or <paramref name="target"/> itself for an empty selector.</returns>
		/// <exception cref="SelectorParseException">
		/// In the non-generic form, a property doesn't exist on the type it's read from, or a function's arguments turn out
		/// to have the wrong types.
		/// </exception>
		/// <example>
		/// <code>
		/// var order = Expression.Parameter(typeof(Order), "o");
		/// var body = SelectorExpressions.CreateMemberAccess(order, (PropertySelector&lt;Order&gt;)"Lines.Amount|$FN_AGGR_SUM");
		/// var total = Expression.Lambda&lt;Func&lt;Order, decimal&gt;&gt;(body, order); // o => o.Lines.Select(y => y.Amount).Sum()
		/// </code>
		/// </example>
		public static Expression CreateMemberAccess(Expression? target, PropertySelector selector)
		{
			if (selector is null || string.IsNullOrEmpty(selector.PlainValue)) return target!;

			var previousRoot = _scopeRoot;
			_scopeRoot = target;
			try
			{
				var temp = selector;

				Expression memberAccess = target!;
				while (temp is not null)
				{
					memberAccess = temp.PropertyAccess(memberAccess);
					temp = temp.ChildSelector;
				}

				return memberAccess;
			}
			finally
			{
				_scopeRoot = previousRoot;
			}
		}

		internal static ParameterExpression? FindRootParameter(Expression? expression) => expression switch
		{
			ParameterExpression parameter => parameter,
			MemberExpression member => FindRootParameter(member.Expression),
			// Extension method calls (Select, FirstOrDefault, …) carry the source as the
			// first argument, instance calls carry it as the object.
			MethodCallExpression call => FindRootParameter(call.Object) ?? FirstRootParameter(call.Arguments),
			UnaryExpression unary => FindRootParameter(unary.Operand),
			BinaryExpression binary => FindRootParameter(binary.Left) ?? FindRootParameter(binary.Right),
			ConditionalExpression conditional => FindRootParameter(conditional.Test) ?? FindRootParameter(conditional.IfTrue) ?? FindRootParameter(conditional.IfFalse),
			NewArrayExpression array => FirstRootParameter(array.Expressions),
			_ => null,
		};

		private static ParameterExpression? FirstRootParameter(IEnumerable<Expression> expressions)
		{
			foreach (var expression in expressions)
			{
				if (FindRootParameter(expression) is { } parameter)
					return parameter;
			}

			return null;
		}

		internal static Expression CreateMemberAccessRec(Expression target, string[] parts, int index)
		{
			Expression current = target;

			for (int i = index; i < parts.Length; i++)
			{
				var part = parts[i].StartsWith(SelectorDefaults.PropertyNameEscape)
					? Extensions.TypesExtensions.UnescapePropertyName(parts[i], string.Join('.', parts), parts.Take(i).Sum(p => p.Length + 1))
					: parts[i];
				var propertyInfo = Extensions.TypesExtensions.FindProperty(current.Type, part);

				if (propertyInfo is null)
				{
					if (Nullable.GetUnderlyingType(current.Type) is not null)
						return CreateLiftedMemberAccess(current, parts, i);

					if (!TryGetGroupingTypes(target.Type, out var _, out var groupElementType) || groupElementType is null)
					{
						if (i != index || !TryGetEnumerableElementType(target.Type, out groupElementType) || groupElementType is null)
							throw Extensions.TypesExtensions.PropertyNotFound(part, current.Type, string.Join('.', parts), parts.Take(i).Sum(p => p.Length + 1) + parts[i].Length - part.Length);
					}

					var param = Expression.Parameter(groupElementType, "y");
					var innerAccess = CreateMemberAccessRec(param, parts, index);

					if (TryGetEnumerableElementType(innerAccess.Type, out var innerElement) && innerElement != null)
					{
						var enumerableOfResultType = typeof(IEnumerable<>).MakeGenericType(innerElement);
						var lambdaType = typeof(Func<,>).MakeGenericType(param.Type, enumerableOfResultType);
						var lambda = Expression.Lambda(lambdaType, innerAccess, param);

						var method = MethodInfos.SelectManyMethodInfo.MakeGenericMethod(groupElementType, innerElement);
						current = Expression.Call(method, target, lambda);
					}
					else
					{
						var lambda = Expression.Lambda(innerAccess, param);
						var resultType = innerAccess.Type;
						var method = MethodInfos.SelectMethodInfo.MakeGenericMethod(groupElementType, resultType);

						current = Expression.Call(method, target, lambda);
					}

					break;
				}

				var property = Expression.Property(current, propertyInfo);
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

						var method = MethodInfos.SelectManyMethodInfo.MakeGenericMethod(elementType, innerElement);
						current = Expression.Call(method, property, lambda);
					}
					else
					{
						var lambda = Expression.Lambda(innerAccess, param);
						var resultType = innerAccess.Type;
						var method = MethodInfos.SelectMethodInfo.MakeGenericMethod(elementType, resultType);

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

		private static Expression CreateLiftedMemberAccess(Expression nullable, string[] parts, int index)
		{
			var inner = CreateMemberAccessRec(Expression.Property(nullable, nameof(Nullable<int>.Value)), parts, index);
			var liftedType = LiftToNullable(inner.Type);

			return Expression.Condition(
				Expression.Equal(nullable, Expression.Constant(null, nullable.Type)),
				Expression.Constant(null, liftedType),
				inner.Type == liftedType ? inner : Expression.Convert(inner, liftedType));
		}

		internal static Type LiftToNullable(Type type)
			=> type.IsValueType && Nullable.GetUnderlyingType(type) is null
				? typeof(Nullable<>).MakeGenericType(type)
				: type;

		internal static bool TryGetGroupingTypes(Type type, out Type? keyType, out Type? elementType)
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
