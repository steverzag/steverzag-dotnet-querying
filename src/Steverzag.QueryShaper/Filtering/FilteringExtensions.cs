using Steverzag.PropertySelectors;
using Steverzag.QueryShaper.Configuration;
using Steverzag.QueryShaper.Configuration.Filtering;
using Steverzag.QueryShaper.Exceptions;
using Steverzag.QueryShaper.Utils;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace Steverzag.QueryShaper.Filtering
{
	public static class FilteringExtensions
	{

		private const string METHOD_NAME_CONTAINS = "Contains";
		private const string METHOD_NAME_STARTS_WITH = "StartsWith";
		private const string METHOD_NAME_ENDS_WITH = "EndsWith";
		private const string METHOD_NAME_ANY = "Any";
		private const string METHOD_NAME_ALL = "All";
		private const string METHOD_NAME_EQUALS = "Equals";
		private const string METHOD_NAME_TO_STRING = "ToString";

		//TODO: Replicate for all other methods used above
		private static MethodInfo AnyMethodInfo = typeof(Enumerable)
			.GetMethods()
			.Where(m => m.Name == METHOD_NAME_ANY && m.GetParameters().Length == 2)
			.First();
		private static MethodInfo AllMethodInfo = typeof(Enumerable)
			.GetMethods()
			.Where(m => m.Name == METHOD_NAME_ALL && m.GetParameters().Length == 2)
			.First();
		

		public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, IEnumerable<string>? searchLookups, string? searchValue, IEnumerable<FilterNode> logicFilters)
		{
			if (searchLookups is not null)
			{
				query = query.ApplyBaseFilter(searchLookups, searchValue);
			}

			if (logicFilters is not null)
			{
				query = query.ApplyLogicsFilter(logicFilters);
			}

			return query;
		}

		public static IQueryable<T> ApplyLogicsFilter<T>(this IQueryable<T> query, IEnumerable<FilterNode> logicFilters)
		{
			if (logicFilters is null)
			{
				throw new ArgumentNullException(nameof(logicFilters));
			}
			else if (logicFilters is null || !logicFilters.Any())
			{
				return query;
			}

			var parameterExpressions = Expression.Parameter(typeof(T), name: "x");

			Expression? combinedExpression = null;
			foreach (var filter in logicFilters)
			{

				var filterExpression = BuildFilterExpression(parameterExpressions, filter);

				if (combinedExpression == null)
				{
					combinedExpression = filterExpression;
				}
				else if (filter.FilterLogicOption == LogicalConnector.And)
				{
					combinedExpression = Expression.AndAlso(combinedExpression, filterExpression);
				}
				else
				{
					combinedExpression = Expression.OrElse(combinedExpression, filterExpression);
				}
			}

			var lambda = Expression.Lambda<Func<T, bool>>(combinedExpression!, parameterExpressions);
			Console.WriteLine(ExpressionUtils.ToFlatString(lambda.Body));

			return query.Where(lambda);
		}

		private static Expression BuildFilterExpression(ParameterExpression parameterExpressions, FilterNode filter)
		{

			if (filter.Filters is null || !filter.Filters.Any())
			{

				Expression filterConditionExpression = CreateFilterConditionExpression(parameterExpressions, filter);
				return filterConditionExpression;
			}

			Expression? subCombinedExpression = null;
			foreach (var subFilter in filter.Filters)
			{
				var subFilterExpression = BuildFilterExpression(parameterExpressions, subFilter);
				if (subCombinedExpression == null)
				{
					subCombinedExpression = subFilterExpression;
				}
				else if (subFilter.FilterLogicOption == LogicalConnector.And)
				{
					subCombinedExpression = Expression.AndAlso(subCombinedExpression, subFilterExpression);
				}
				else
				{
					subCombinedExpression = Expression.OrElse(subCombinedExpression, subFilterExpression);
				}
			}

			return subCombinedExpression!;
		}

		private static Expression CreateFilterConditionExpression(ParameterExpression parameterExpressions, FilterNode filter)
		{
			var propertyExpression = ExpressionUtils.CreateMemberAccess(parameterExpressions, filter.FieldSelector);

			var propertyType = Nullable.GetUnderlyingType(propertyExpression.Type);
			var isNullableType = propertyType != null;
			if (!isNullableType)
			{
				propertyType = propertyExpression.Type;
			}

			var isInOrNotInCondition = filter.FilterCondition is FilterCondition.In or FilterCondition.NotIn;

			if (propertyType == typeof(string) && !isInOrNotInCondition)
			{
				string? stringValue = null;
				List<object>? listValue = null;

				if (filter.SearchValue is not null && filter.SearchValue.Value.GetKind() is ResultValueKind.Enumerable)
				{
					listValue = (List<object>)filter.SearchValue.Value;
				}
				else
				{
					stringValue = filter.SearchValue is null ? null : (string)filter.SearchValue;
				}

				return filter.FilterCondition switch
				{
					FilterCondition.Contains when stringValue is not null => CreateContainsExpression(propertyExpression, stringValue),
					FilterCondition.DoesNotContain when stringValue is not null => Expression.Not(CreateContainsExpression(propertyExpression, stringValue)),
					FilterCondition.Equals when stringValue is not null => CreateEqualsExpression(propertyExpression, stringValue),
					FilterCondition.NotEquals when stringValue is not null => Expression.Not(CreateEqualsExpression(propertyExpression, stringValue)),
					FilterCondition.StartsWith when stringValue is not null => CreateStartsWithExpression(propertyExpression, stringValue),
					FilterCondition.DoesNotStartWith when stringValue is not null => Expression.Not(CreateStartsWithExpression(propertyExpression, stringValue)),
					FilterCondition.EndsWith when stringValue is not null => CreateEndsWithExpression(propertyExpression, stringValue),
					FilterCondition.DoesNotEndWith when stringValue is not null => Expression.Not(CreateEndsWithExpression(propertyExpression, stringValue)),
					FilterCondition.IsEmpty => Expression.Equal(Expression.Coalesce(propertyExpression, Expression.Constant(string.Empty)), Expression.Constant(string.Empty)),
					FilterCondition.IsNotEmpty => Expression.Not(Expression.Equal(Expression.Coalesce(propertyExpression, Expression.Constant(string.Empty)), Expression.Constant(string.Empty))),
					_ when stringValue is null => throw new FilterConditionAndValueNotSupportedException($"Null value filtering is only supported for {nameof(FilterCondition.IsEmpty)} and {nameof(FilterCondition.IsNotEmpty)} conditions."),
					_ => throw new FilterNotSupportedException(filter.FilterCondition, propertyExpression.Type)
				};
			}
			else if (propertyType == typeof(Guid) && !isInOrNotInCondition)
			{
				Guid guidValue = filter.SearchValue is null ? Guid.Empty : (Guid)filter.SearchValue;
				var constantType = isNullableType ? typeof(Guid?) : typeof(Guid);
				return filter.FilterCondition switch
				{
					FilterCondition.Equals => Expression.Equal(propertyExpression, Expression.Constant(guidValue, constantType)),
					FilterCondition.NotEquals => Expression.Not(Expression.Equal(propertyExpression, Expression.Constant(guidValue, constantType))),
					FilterCondition.IsEmpty when isNullableType => Expression.Equal(propertyExpression, Expression.Constant(null, typeof(Guid?))),
					FilterCondition.IsNotEmpty when isNullableType => Expression.Not(Expression.Equal(propertyExpression, Expression.Constant(null, typeof(Guid?)))),
					FilterCondition.IsEmpty => throw new FilterConditionAndValueNotSupportedException($"Non-nullable Guid filtering is not supported for {nameof(FilterCondition.IsEmpty)} condition."),
					FilterCondition.IsNotEmpty => throw new FilterConditionAndValueNotSupportedException($"Non-nullable Guid filtering is not supported for {nameof(FilterCondition.IsNotEmpty)} condition."),
					_ => throw new FilterNotSupportedException(filter.FilterCondition, propertyExpression.Type)
				};
			}
			else if (propertyType == typeof(bool) && !isInOrNotInCondition)
			{
				bool boolValue = filter.SearchValue is null ? false : (bool)filter.SearchValue;
				var constantType = isNullableType ? typeof(bool?) : typeof(bool);
				return filter.FilterCondition switch
				{
					FilterCondition.Equals => Expression.Equal(propertyExpression, Expression.Constant(boolValue, constantType)),
					FilterCondition.NotEquals => Expression.Not(Expression.Equal(propertyExpression, Expression.Constant(boolValue, constantType))),
					FilterCondition.IsEmpty when isNullableType => Expression.Equal(propertyExpression, Expression.Constant(null, typeof(bool?))),
					FilterCondition.IsNotEmpty when isNullableType => Expression.Not(Expression.Equal(propertyExpression, Expression.Constant(null, typeof(bool?)))),
					FilterCondition.IsEmpty => throw new FilterConditionAndValueNotSupportedException($"Non-nullable bool filtering is not supported for {nameof(FilterCondition.IsEmpty)} condition."),
					FilterCondition.IsNotEmpty => throw new FilterConditionAndValueNotSupportedException($"Non-nullable bool filtering is not supported for {nameof(FilterCondition.IsNotEmpty)} condition."),
					_ => throw new FilterNotSupportedException(filter.FilterCondition, propertyExpression.Type)
				};
			}
			else if (propertyType == typeof(byte) && !isInOrNotInCondition)
			{
				byte byteValue = filter.SearchValue is null ? (byte)0 : (byte)filter.SearchValue;

				var constantType = isNullableType ? typeof(byte?) : typeof(byte);
				return filter.FilterCondition switch
				{
					FilterCondition.GreaterThan => Expression.GreaterThan(propertyExpression, Expression.Constant(byteValue, constantType)),
					FilterCondition.GreaterThanOrEqual => Expression.GreaterThanOrEqual(propertyExpression, Expression.Constant(byteValue, constantType)),
					FilterCondition.LessThan => Expression.LessThan(propertyExpression, Expression.Constant(byteValue, constantType)),
					FilterCondition.LessThanOrEqual => Expression.LessThanOrEqual(propertyExpression, Expression.Constant(byteValue, constantType)),
					FilterCondition.Equals => Expression.Equal(propertyExpression, Expression.Constant(byteValue, constantType)),
					FilterCondition.NotEquals => Expression.Not(Expression.Equal(propertyExpression, Expression.Constant(byteValue, constantType))),
					FilterCondition.IsEmpty when isNullableType => Expression.Equal(Expression.Coalesce(propertyExpression, Expression.Constant((byte)0)), Expression.Constant((byte)0)),
					FilterCondition.IsNotEmpty when isNullableType => Expression.Not(Expression.Equal(Expression.Coalesce(propertyExpression, Expression.Constant((byte)0)), Expression.Constant((byte)0))),
					FilterCondition.IsEmpty => Expression.Equal(propertyExpression, Expression.Constant((byte)0)),
					FilterCondition.IsNotEmpty => Expression.Not(Expression.Equal(propertyExpression, Expression.Constant((byte)0))),
					_ => throw new FilterNotSupportedException(filter.FilterCondition, propertyExpression.Type)
				};
			}
			else if (propertyType == typeof(short) && !isInOrNotInCondition)
			{
				short shortValue = filter.SearchValue is null ? (short)0 : (short)filter.SearchValue;

				var constantType = isNullableType ? typeof(short?) : typeof(short);
				return filter.FilterCondition switch
				{
					FilterCondition.GreaterThan => Expression.GreaterThan(propertyExpression, Expression.Constant(shortValue, constantType)),
					FilterCondition.GreaterThanOrEqual => Expression.GreaterThanOrEqual(propertyExpression, Expression.Constant(shortValue, constantType)),
					FilterCondition.LessThan => Expression.LessThan(propertyExpression, Expression.Constant(shortValue, constantType)),
					FilterCondition.LessThanOrEqual => Expression.LessThanOrEqual(propertyExpression, Expression.Constant(shortValue, constantType)),
					FilterCondition.Equals => Expression.Equal(propertyExpression, Expression.Constant(shortValue, constantType)),
					FilterCondition.NotEquals => Expression.Not(Expression.Equal(propertyExpression, Expression.Constant(shortValue, constantType))),
					FilterCondition.IsEmpty when isNullableType => Expression.Equal(Expression.Coalesce(propertyExpression, Expression.Constant((short)0)), Expression.Constant((short)0)),
					FilterCondition.IsNotEmpty when isNullableType => Expression.Not(Expression.Equal(Expression.Coalesce(propertyExpression, Expression.Constant((short)0)), Expression.Constant((short)0))),
					FilterCondition.IsEmpty => Expression.Equal(propertyExpression, Expression.Constant((short)0)),
					FilterCondition.IsNotEmpty => Expression.Not(Expression.Equal(propertyExpression, Expression.Constant((short)0))),
					_ => throw new FilterNotSupportedException(filter.FilterCondition, propertyExpression.Type)
				};
			}
			else if (propertyType == typeof(int) && !isInOrNotInCondition)
			{
				int intValue = filter.SearchValue is null ? 0 : (int)filter.SearchValue;

				var constantType = isNullableType ? typeof(int?) : typeof(int);
				return filter.FilterCondition switch
				{
					FilterCondition.GreaterThan => Expression.GreaterThan(propertyExpression, Expression.Constant(intValue, constantType)),
					FilterCondition.GreaterThanOrEqual => Expression.GreaterThanOrEqual(propertyExpression, Expression.Constant(intValue, constantType)),
					FilterCondition.LessThan => Expression.LessThan(propertyExpression, Expression.Constant(intValue, constantType)),
					FilterCondition.LessThanOrEqual => Expression.LessThanOrEqual(propertyExpression, Expression.Constant(intValue, constantType)),
					FilterCondition.Equals => Expression.Equal(propertyExpression, Expression.Constant(intValue, constantType)),
					FilterCondition.NotEquals => Expression.Not(Expression.Equal(propertyExpression, Expression.Constant(intValue, constantType))),
					FilterCondition.IsEmpty when isNullableType => Expression.Equal(Expression.Coalesce(propertyExpression, Expression.Constant(0)), Expression.Constant(0)),
					FilterCondition.IsNotEmpty when isNullableType => Expression.Not(Expression.Equal(Expression.Coalesce(propertyExpression, Expression.Constant(0)), Expression.Constant(0))),
					FilterCondition.IsEmpty => Expression.Equal(propertyExpression, Expression.Constant(0)),
					FilterCondition.IsNotEmpty => Expression.Not(Expression.Equal(propertyExpression, Expression.Constant(0))),
					_ => throw new FilterNotSupportedException(filter.FilterCondition, propertyExpression.Type)
				};
			}
			else if (propertyType == typeof(long) && !isInOrNotInCondition)
			{
				long longValue = filter.SearchValue is null ? 0L : (long)filter.SearchValue;

				var constantType = isNullableType ? typeof(long?) : typeof(long);
				return filter.FilterCondition switch
				{
					FilterCondition.GreaterThan => Expression.GreaterThan(propertyExpression, Expression.Constant(longValue, constantType)),
					FilterCondition.GreaterThanOrEqual => Expression.GreaterThanOrEqual(propertyExpression, Expression.Constant(longValue, constantType)),
					FilterCondition.LessThan => Expression.LessThan(propertyExpression, Expression.Constant(longValue, constantType)),
					FilterCondition.LessThanOrEqual => Expression.LessThanOrEqual(propertyExpression, Expression.Constant(longValue, constantType)),
					FilterCondition.Equals => Expression.Equal(propertyExpression, Expression.Constant(longValue, constantType)),
					FilterCondition.NotEquals => Expression.Not(Expression.Equal(propertyExpression, Expression.Constant(longValue, constantType))),
					FilterCondition.IsEmpty when isNullableType => Expression.Equal(Expression.Coalesce(propertyExpression, Expression.Constant(0L)), Expression.Constant(0L)),
					FilterCondition.IsNotEmpty when isNullableType => Expression.Not(Expression.Equal(Expression.Coalesce(propertyExpression, Expression.Constant(0L)), Expression.Constant(0L))),
					FilterCondition.IsEmpty => Expression.Equal(propertyExpression, Expression.Constant(0L)),
					FilterCondition.IsNotEmpty => Expression.Not(Expression.Equal(propertyExpression, Expression.Constant(0L))),
					_ => throw new FilterNotSupportedException(filter.FilterCondition, propertyExpression.Type)
				};
			}
			else if (propertyType == typeof(float) && !isInOrNotInCondition)
			{
				float floatValue = filter.SearchValue is null ? 0f : (float)filter.SearchValue;

				var constantType = isNullableType ? typeof(float?) : typeof(float);
				return filter.FilterCondition switch
				{
					FilterCondition.GreaterThan => Expression.GreaterThan(propertyExpression, Expression.Constant(floatValue, constantType)),
					FilterCondition.GreaterThanOrEqual => Expression.GreaterThanOrEqual(propertyExpression, Expression.Constant(floatValue, constantType)),
					FilterCondition.LessThan => Expression.LessThan(propertyExpression, Expression.Constant(floatValue, constantType)),
					FilterCondition.LessThanOrEqual => Expression.LessThanOrEqual(propertyExpression, Expression.Constant(floatValue, constantType)),
					FilterCondition.Equals => Expression.Equal(propertyExpression, Expression.Constant(floatValue, constantType)),
					FilterCondition.NotEquals => Expression.Not(Expression.Equal(propertyExpression, Expression.Constant(floatValue, constantType))),
					FilterCondition.IsEmpty when isNullableType => Expression.Equal(Expression.Coalesce(propertyExpression, Expression.Constant(0f)), Expression.Constant(0f)),
					FilterCondition.IsNotEmpty when isNullableType => Expression.Not(Expression.Equal(Expression.Coalesce(propertyExpression, Expression.Constant(0f)), Expression.Constant(0f))),
					FilterCondition.IsEmpty => Expression.Equal(propertyExpression, Expression.Constant(0f)),
					FilterCondition.IsNotEmpty => Expression.Not(Expression.Equal(propertyExpression, Expression.Constant(0f))),
					_ => throw new FilterNotSupportedException(filter.FilterCondition, propertyExpression.Type)
				};
			}
			else if (propertyType == typeof(double) && !isInOrNotInCondition)
			{
				double doubleValue = filter.SearchValue is null ? 0d : (double)filter.SearchValue;

				var constantType = isNullableType ? typeof(double?) : typeof(double);
				return filter.FilterCondition switch
				{
					FilterCondition.GreaterThan => Expression.GreaterThan(propertyExpression, Expression.Constant(doubleValue, constantType)),
					FilterCondition.GreaterThanOrEqual => Expression.GreaterThanOrEqual(propertyExpression, Expression.Constant(doubleValue, constantType)),
					FilterCondition.LessThan => Expression.LessThan(propertyExpression, Expression.Constant(doubleValue, constantType)),
					FilterCondition.LessThanOrEqual => Expression.LessThanOrEqual(propertyExpression, Expression.Constant(doubleValue, constantType)),
					FilterCondition.Equals => Expression.Equal(propertyExpression, Expression.Constant(doubleValue, constantType)),
					FilterCondition.NotEquals => Expression.Not(Expression.Equal(propertyExpression, Expression.Constant(doubleValue, constantType))),
					FilterCondition.IsEmpty when isNullableType => Expression.Equal(Expression.Coalesce(propertyExpression, Expression.Constant(0d)), Expression.Constant(0d)),
					FilterCondition.IsNotEmpty when isNullableType => Expression.Not(Expression.Equal(Expression.Coalesce(propertyExpression, Expression.Constant(0d)), Expression.Constant(0d))),
					FilterCondition.IsEmpty => Expression.Equal(propertyExpression, Expression.Constant(0d)),
					FilterCondition.IsNotEmpty => Expression.Not(Expression.Equal(propertyExpression, Expression.Constant(0d))),
					_ => throw new FilterNotSupportedException(filter.FilterCondition, propertyExpression.Type)
				};
			}
			else if (propertyType == typeof(decimal) && !isInOrNotInCondition)
			{
				decimal decimalValue = filter.SearchValue is null ? 0m : (decimal)filter.SearchValue;

				var constantType = isNullableType ? typeof(decimal?) : typeof(decimal);
				return filter.FilterCondition switch
				{
					FilterCondition.GreaterThan => Expression.GreaterThan(propertyExpression, Expression.Constant(decimalValue, constantType)),
					FilterCondition.GreaterThanOrEqual => Expression.GreaterThanOrEqual(propertyExpression, Expression.Constant(decimalValue, constantType)),
					FilterCondition.LessThan => Expression.LessThan(propertyExpression, Expression.Constant(decimalValue, constantType)),
					FilterCondition.LessThanOrEqual => Expression.LessThanOrEqual(propertyExpression, Expression.Constant(decimalValue, constantType)),
					FilterCondition.Equals => Expression.Equal(propertyExpression, Expression.Constant(decimalValue, constantType)),
					FilterCondition.NotEquals => Expression.Not(Expression.Equal(propertyExpression, Expression.Constant(decimalValue, constantType))),
					FilterCondition.IsEmpty when isNullableType => Expression.Equal(Expression.Coalesce(propertyExpression, Expression.Constant(0m)), Expression.Constant(0m)),
					FilterCondition.IsNotEmpty when isNullableType => Expression.Not(Expression.Equal(Expression.Coalesce(propertyExpression, Expression.Constant(0m)), Expression.Constant(0m))),
					FilterCondition.IsEmpty => Expression.Equal(propertyExpression, Expression.Constant(0m)),
					FilterCondition.IsNotEmpty => Expression.Not(Expression.Equal(propertyExpression, Expression.Constant(0m))),
					_ => throw new FilterNotSupportedException(filter.FilterCondition, propertyExpression.Type)
				};
			}
			else if (propertyType == typeof(DateTime) && !isInOrNotInCondition)
			{
				ConstantExpression Value() => Expression.Constant((DateTime)filter.SearchValue!.Value, propertyExpression.Type);
				return filter.FilterCondition switch
				{
					FilterCondition.GreaterThan => Expression.GreaterThan(propertyExpression, Value()),
					FilterCondition.GreaterThanOrEqual => Expression.GreaterThanOrEqual(propertyExpression, Value()),
					FilterCondition.LessThan => Expression.LessThan(propertyExpression, Value()),
					FilterCondition.LessThanOrEqual => Expression.LessThanOrEqual(propertyExpression, Value()),
					FilterCondition.Equals => Expression.Equal(propertyExpression, Value()),
					FilterCondition.NotEquals => Expression.Not(Expression.Equal(propertyExpression, Value())),
					FilterCondition.IsEmpty when isNullableType => Expression.Equal(propertyExpression, Expression.Constant(null, typeof(DateTime?))),
					FilterCondition.IsNotEmpty when isNullableType => Expression.Not(Expression.Equal(propertyExpression, Expression.Constant(null, typeof(DateTime?)))),
					FilterCondition.IsEmpty => throw new FilterConditionAndValueNotSupportedException($"Non-nullable DateTime filtering is not supported for {nameof(FilterCondition.IsEmpty)} condition."),
					FilterCondition.IsNotEmpty => throw new FilterConditionAndValueNotSupportedException($"Non-nullable DateTime filtering is not supported for {nameof(FilterCondition.IsNotEmpty)} condition."),
					_ => throw new FilterNotSupportedException(filter.FilterCondition, propertyExpression.Type)
				};
			}
			else if (propertyType == typeof(DateOnly) && !isInOrNotInCondition)
			{
				ConstantExpression Value() => Expression.Constant((DateOnly)filter.SearchValue!.Value, propertyExpression.Type);
				return filter.FilterCondition switch
				{
					FilterCondition.GreaterThan => Expression.GreaterThan(propertyExpression, Value()),
					FilterCondition.GreaterThanOrEqual => Expression.GreaterThanOrEqual(propertyExpression, Value()),
					FilterCondition.LessThan => Expression.LessThan(propertyExpression, Value()),
					FilterCondition.LessThanOrEqual => Expression.LessThanOrEqual(propertyExpression, Value()),
					FilterCondition.Equals => Expression.Equal(propertyExpression, Value()),
					FilterCondition.NotEquals => Expression.Not(Expression.Equal(propertyExpression, Value())),
					FilterCondition.IsEmpty when isNullableType => Expression.Equal(propertyExpression, Expression.Constant(null, typeof(DateOnly?))),
					FilterCondition.IsNotEmpty when isNullableType => Expression.Not(Expression.Equal(propertyExpression, Expression.Constant(null, typeof(DateOnly?)))),
					FilterCondition.IsEmpty => throw new FilterConditionAndValueNotSupportedException($"Non-nullable DateOnly filtering is not supported for {nameof(FilterCondition.IsEmpty)} condition."),
					FilterCondition.IsNotEmpty => throw new FilterConditionAndValueNotSupportedException($"Non-nullable DateOnly filtering is not supported for {nameof(FilterCondition.IsNotEmpty)} condition."),
					_ => throw new FilterNotSupportedException(filter.FilterCondition, propertyExpression.Type)
				};
			}
			else if (propertyType == typeof(TimeOnly) && !isInOrNotInCondition)
			{
				ConstantExpression Value() => Expression.Constant((TimeOnly)filter.SearchValue!.Value, propertyExpression.Type);
				return filter.FilterCondition switch
				{
					FilterCondition.GreaterThan => Expression.GreaterThan(propertyExpression, Value()),
					FilterCondition.GreaterThanOrEqual => Expression.GreaterThanOrEqual(propertyExpression, Value()),
					FilterCondition.LessThan => Expression.LessThan(propertyExpression, Value()),
					FilterCondition.LessThanOrEqual => Expression.LessThanOrEqual(propertyExpression, Value()),
					FilterCondition.Equals => Expression.Equal(propertyExpression, Value()),
					FilterCondition.NotEquals => Expression.Not(Expression.Equal(propertyExpression, Value())),
					FilterCondition.IsEmpty when isNullableType => Expression.Equal(propertyExpression, Expression.Constant(null, typeof(TimeOnly?))),
					FilterCondition.IsNotEmpty when isNullableType => Expression.Not(Expression.Equal(propertyExpression, Expression.Constant(null, typeof(TimeOnly?)))),
					FilterCondition.IsEmpty => throw new FilterConditionAndValueNotSupportedException($"Non-nullable TimeOnly filtering is not supported for {nameof(FilterCondition.IsEmpty)} condition."),
					FilterCondition.IsNotEmpty => throw new FilterConditionAndValueNotSupportedException($"Non-nullable TimeOnly filtering is not supported for {nameof(FilterCondition.IsNotEmpty)} condition."),
					_ => throw new FilterNotSupportedException(filter.FilterCondition, propertyExpression.Type)
				};
			}
			else if (propertyType!.IsEnum && !isInOrNotInCondition)
			{

				if (filter.SearchValue is null && !isNullableType)
				{
					throw new Exception("Null value provided for non-nullable enum type " + propertyType.Name);
				}

				Type enumUnderlyingType = Enum.GetUnderlyingType(propertyType);
				object? enumUnderlayingValue = filter.SearchValue is null ? null
					: filter.SearchValue?.GetKind() switch
					{
						ResultValueKind.Integer => (int?)filter.SearchValue!,
						ResultValueKind.Long => (long?)filter.SearchValue!,
						_ => throw new Exception("Unsupported value type for enum filtering: " + filter.SearchValue?.GetKind().ToString()),
					};

				object? enumValue = null;
				var isInvalidValueProvided = enumUnderlayingValue is not null
					&& !Enum.TryParse(propertyType, enumUnderlayingValue.ToString(), out enumValue);

				if (isInvalidValueProvided)
				{
					throw new Exception($"Invalid value ({enumUnderlayingValue ?? "null"}) provided for type {propertyType.Name}");
				}
				var enumConstant = enumUnderlayingValue is null
					? Expression.Constant(null, propertyExpression.Type)
					: Expression.Constant(enumValue, propertyExpression.Type);

				return filter.FilterCondition switch
				{
					FilterCondition.Equals when enumValue is not null => Expression.Equal(propertyExpression, enumConstant),
					FilterCondition.NotEquals when enumValue is not null => Expression.Not(Expression.Equal(propertyExpression, enumConstant)),
					FilterCondition.IsEmpty when enumValue is null => Expression.Equal(propertyExpression, Expression.Constant(Activator.CreateInstance(propertyExpression.Type))),
					FilterCondition.IsNotEmpty when enumValue is null => Expression.Not(Expression.Equal(propertyExpression, Expression.Constant(Activator.CreateInstance(propertyExpression.Type)))),
					_ when enumValue is null => throw new FilterConditionAndValueNotSupportedException($"Null enum filtering is only supported for {nameof(FilterCondition.IsEmpty)} and {nameof(FilterCondition.IsNotEmpty)} conditions."),
					_ => throw new FilterNotSupportedException(filter.FilterCondition, propertyExpression.Type)
				};
			}
			else if (isInOrNotInCondition)
			{
				List<object> valuesList = (List<object>)filter.SearchValue!;

				return filter.FilterCondition switch
				{
					FilterCondition.In => CreateInExpression(propertyExpression, valuesList, filter.FilterCondition),
					FilterCondition.NotIn => Expression.Not(CreateInExpression(propertyExpression, valuesList, filter.FilterCondition)),
					_ => throw new FilterNotSupportedException(filter.FilterCondition, propertyExpression.Type)
				};
			}
			else if (ExpressionUtils.TryGetEnumerableElementType(propertyType, out var elementType))
			{
				return filter.FilterCondition switch
				{
					FilterCondition.Contains when elementType == typeof(string) => CreateEnumerableContainsExpression(propertyExpression, (string)filter.SearchValue!),
					FilterCondition.DoesNotContain when elementType == typeof(string) => Expression.Not(CreateEnumerableContainsExpression(propertyExpression, (string)filter.SearchValue!)),
					FilterCondition.ContainsAny => CreateContainsAnyExpression(propertyExpression, elementType, (List<object>)filter.SearchValue!, filter.FilterCondition),
					FilterCondition.DoesNotContainAny => Expression.Not(CreateContainsAnyExpression(propertyExpression, elementType, (List<object>)filter.SearchValue!, filter.FilterCondition)),
					FilterCondition.ContainsAll => CreateContainsAllExpression(propertyExpression, elementType, (List<object>)filter.SearchValue!, filter.FilterCondition),
					FilterCondition.DoesNotContainAll => Expression.Not(CreateContainsAllExpression(propertyExpression, elementType, (List<object>)filter.SearchValue!, filter.FilterCondition)),
					FilterCondition.IsEmpty => Expression.Equal(Expression.Call(typeof(Enumerable), METHOD_NAME_ANY, [elementType!], propertyExpression), Expression.Constant(false)),
					FilterCondition.IsNotEmpty => Expression.Equal(Expression.Call(typeof(Enumerable), METHOD_NAME_ANY, [elementType!], propertyExpression), Expression.Constant(true)),
					_ => throw new FilterNotSupportedException(filter.FilterCondition, propertyType)
				};
			}
			else
			{
				throw new FilterNotSupportedException(propertyExpression.Type);
			}
		}

		private static MethodCallExpression CreateContainsAnyExpression(Expression propertyExpression, Type? elementType, List<object> valuesList, FilterCondition condition)
		{
			ArgumentNullException.ThrowIfNull(elementType, nameof(elementType));
			ArgumentNullException.ThrowIfNull(valuesList, nameof(valuesList));
			if (valuesList.Count == 0)
			{
				throw new ArgumentException("Values List must contain at least one value", nameof(valuesList));
			}

			// x.Collection.Any(y => y == v1 || y == v2 || ...)
			BinaryExpression? combinedExpression = null;
			ParameterExpression parameter = Expression.Parameter(elementType, "y");

			foreach (var value in CreateTypedValueList(elementType, valuesList, condition))
			{
				var equalsValue = Expression.Equal(parameter, Expression.Constant(value, elementType));
				combinedExpression = combinedExpression is null
					? equalsValue
					: Expression.OrElse(combinedExpression, equalsValue);
			}

			var anyMethod = AnyMethodInfo.MakeGenericMethod(elementType);
			var lambda = Expression.Lambda(combinedExpression!, parameter);
			return Expression.Call(anyMethod, propertyExpression, lambda);
		}

		private static MethodCallExpression CreateContainsAllExpression(Expression propertyExpression, Type? elementType, List<object> valuesList, FilterCondition condition)
		{
			ArgumentNullException.ThrowIfNull(elementType, nameof(elementType));
			ArgumentNullException.ThrowIfNull(valuesList, nameof(valuesList));
			if (!valuesList.Any())
			{
				throw new ArgumentException("Values List must contain at least one value", nameof(valuesList));
			}

			ParameterExpression parameter = Expression.Parameter(elementType, "y");
			ParameterExpression parameter2 = Expression.Parameter(elementType, "z");

			var list = CreateTypedValueList(elementType, valuesList, condition);

			var allMethod = AllMethodInfo.MakeGenericMethod(elementType);
			var anyMethod = AnyMethodInfo.MakeGenericMethod(elementType);

			var anyLambda = Expression.Lambda(Expression.Equal(parameter, parameter2), parameter2);
			var anyMethodCall = Expression.Call(anyMethod, propertyExpression, anyLambda);

			var allLambda = Expression.Lambda(anyMethodCall, parameter);
			var allMethodCall = Expression.Call(allMethod, Expression.Constant(list), allLambda);

			return allMethodCall;
		}

		private static MethodCallExpression CreateInExpression(Expression propertyExpression, List<object> valuesList, FilterCondition condition)
		{
			ArgumentNullException.ThrowIfNull(valuesList, nameof(valuesList));
			if (valuesList.Count == 0)
			{
				throw new ArgumentException("Values List must contain at least one value", nameof(valuesList));
			}

			// The list's item type must match the property exactly (int? for an int? property),
			// or Enumerable.Contains can't be called with it.
			var itemType = propertyExpression.Type;
			var list = CreateTypedValueList(itemType, valuesList, condition);

			var containsMethod = typeof(Enumerable).GetMethods()
				.Where(m => m.Name == METHOD_NAME_CONTAINS && m.GetParameters().Length == 2)
				.First()
				.MakeGenericMethod(itemType);
			var containsMethodCall = Expression.Call(containsMethod, Expression.Constant(list), propertyExpression);

			return containsMethodCall;
		}

		/// <summary>
		/// Turns the request's untyped values into a <c>List&lt;itemType&gt;</c>, which can be used as a
		/// query constant. Shared by In/NotIn, ContainsAny and ContainsAll.
		/// <list type="bullet">
		/// <item>Only scalar item types (and their nullable forms) are supported. Anything else, such as an
		/// entity or a nested collection, throws <see cref="FilterNotSupportedException"/>.</item>
		/// <item>Enum values are parsed with <see cref="Enum.TryParse(Type, string?, out object?)"/>, so both
		/// underlying numbers and names are accepted.</item>
		/// <item>Other values must already be boxed as the exact type: no numeric widening or narrowing.</item>
		/// <item>null is accepted only when the item type can hold it.</item>
		/// </list>
		/// </summary>
		private static IList CreateTypedValueList(Type itemType, List<object> values, FilterCondition condition)
		{
			var underlyingType = Nullable.GetUnderlyingType(itemType) ?? itemType;
			if (!IsScalarType(underlyingType))
			{
				throw new FilterNotSupportedException(condition, itemType);
			}

			var acceptsNull = !itemType.IsValueType || underlyingType != itemType;
			var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType))!;

			foreach (var value in values)
			{
				if (value is null)
				{
					list.Add(acceptsNull
						? null
						: throw new FilterConditionAndValueNotSupportedException(
							$"A null value in the list is not supported for non-nullable type {itemType.Name}."));
				}
				else if (underlyingType.IsEnum)
				{
					list.Add(Enum.TryParse(underlyingType, value.ToString(), out var enumValue)
						? enumValue
						: throw new Exception($"Invalid value ({value}) provided for type {underlyingType.Name}"));
				}
				else if (underlyingType.IsInstanceOfType(value))
				{
					list.Add(value);
				}
				else
				{
					throw new InvalidCastException(
						$"The value '{value}' of type {value.GetType().Name} cannot be used for type {itemType.Name}.");
				}
			}

			return list;
		}

		private static bool IsScalarType(Type type) =>
			type.IsPrimitive
			|| type.IsEnum
			|| type == typeof(string)
			|| type == typeof(decimal)
			|| type == typeof(Guid)
			|| type == typeof(DateTime)
			|| type == typeof(DateOnly)
			|| type == typeof(TimeOnly)
			|| type == typeof(TimeSpan)
			|| type == typeof(DateTimeOffset);

		public static IQueryable<T> ApplyBaseFilter<T>(this IQueryable<T> query, string? searchValue, IBaseSearchConfiguration? configuration = default)
		{
			configuration ??= QueryShaperServices.GetServiceOrNull<IBaseSearchConfiguration>();

			if (configuration is null)
			{
				throw new InvalidOperationException(
					$"No {nameof(IBaseSearchConfiguration)} was provided and none has been configured for type {typeof(T).Name}. " +
					$"Pass one explicitly or register it via AddQueryShaper.");
			}

			if (string.IsNullOrWhiteSpace(searchValue) || !configuration.TryGetSearchPredicate<T>(searchValue, out var predicate))
			{
				return query;
			}

			return query.Where(predicate!);
		}

		public static IQueryable<T> ApplyBaseFilter<T>(this IQueryable<T> query, IEnumerable<string>? searchLookups, string? searchValue)
		{
			if (searchLookups is null)
			{
				throw new ArgumentNullException(nameof(searchLookups));
			}
			else if (string.IsNullOrWhiteSpace(searchValue) || !searchLookups.Any())
			{
				return query;
			}

			var lambda = CreateBaseSearchLambdaExpression<T>(searchLookups, searchValue);

			return query.Where(lambda);
		}

		public static Expression<Func<T, bool>> CreateBaseSearchLambdaExpression<T>(IEnumerable<string>? searchLookups, string? searchValue)
		{
			ArgumentNullException.ThrowIfNull(searchLookups);
			if (!searchLookups.Any())
			{
				throw new ArgumentException("At least one search lookup is required.", nameof(searchLookups));
			}

			var parameterExpressio = Expression.Parameter(typeof(T), "x");
			var searchValueConstantExpression = Expression.Constant(searchValue, typeof(string));

			Expression? combinedExpression = null;

			foreach (var field in searchLookups)
			{
				var propertyExpression = ExpressionUtils.CreateMemberAccess(parameterExpressio, field);
				var containingExpression = CreateContainsExpression(propertyExpression, searchValueConstantExpression);
				if (combinedExpression == null)
				{
					combinedExpression = containingExpression;
				}
				else
				{
					combinedExpression = Expression.OrElse(combinedExpression, containingExpression);
				}
			}

			return Expression.Lambda<Func<T, bool>>(combinedExpression!, parameterExpressio);
		}


		private static MethodCallExpression CreateContainsExpression(Expression propertyExpression, ConstantExpression constantExpression)
		{
			var method = typeof(string).GetMethod(METHOD_NAME_CONTAINS, [typeof(string)])!;

			if (propertyExpression.Type != typeof(string))
				propertyExpression = Expression.Call(propertyExpression, METHOD_NAME_TO_STRING, Type.EmptyTypes);

			return Expression.Call(propertyExpression, method, constantExpression);
		}

		private static MethodCallExpression CreateContainsExpression(Expression propertyExpression, string value)
		{
			var method = typeof(string).GetMethod(METHOD_NAME_CONTAINS, [typeof(string)])!;

			if (propertyExpression.Type != typeof(string))
				propertyExpression = Expression.Call(propertyExpression, METHOD_NAME_TO_STRING, Type.EmptyTypes);

			return Expression.Call(propertyExpression, method, Expression.Constant(value, typeof(string)));
		}

		private static MethodCallExpression CreateEnumerableContainsExpression(Expression propertyExpression, string value)
		{
			var anyMethod = AnyMethodInfo.MakeGenericMethod(typeof(string));
			var containMethod = typeof(string).GetMethod(METHOD_NAME_CONTAINS, [typeof(string)])!;

			var parameter = Expression.Parameter(typeof(string), "y");

			var containsMethodCall = Expression.Call(parameter, containMethod, Expression.Constant(value, typeof(string)));
			var lambda = Expression.Lambda(containsMethodCall, parameter);
			var anyMethodCall = Expression.Call(anyMethod, propertyExpression, lambda);
			return anyMethodCall;
		}

		private static MethodCallExpression CreateEqualsExpression(Expression propertyExpression, string value)
		{
			var method = typeof(string).GetMethod(METHOD_NAME_EQUALS, [typeof(string)])!;

			if (propertyExpression.Type != typeof(string))
				propertyExpression = Expression.Call(propertyExpression, METHOD_NAME_TO_STRING, Type.EmptyTypes);

			return Expression.Call(propertyExpression, method, Expression.Constant(value, typeof(string)));
		}

		private static MethodCallExpression CreateStartsWithExpression(Expression propertyExpression, string value)
		{
			var method = typeof(string).GetMethod(METHOD_NAME_STARTS_WITH, [typeof(string)])!;

			if (propertyExpression.Type != typeof(string))
				propertyExpression = Expression.Call(propertyExpression, METHOD_NAME_TO_STRING, Type.EmptyTypes);

			return Expression.Call(propertyExpression, method, Expression.Constant(value, typeof(string)));
		}

		private static MethodCallExpression CreateEndsWithExpression(Expression propertyExpression, string value)
		{
			var method = typeof(string).GetMethod(METHOD_NAME_ENDS_WITH, [typeof(string)])!;

			if (propertyExpression.Type != typeof(string))
				propertyExpression = Expression.Call(propertyExpression, METHOD_NAME_TO_STRING, Type.EmptyTypes);

			return Expression.Call(propertyExpression, method, Expression.Constant(value, typeof(string)));
		}
	}
}
