using ExpDataWorker.PropertySelectors;
using ExpDataWorkerBeta.Feature.Exceptions;
using ExpDataWorkerBeta.Feature.Utils;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace ExpDataWorkerBeta.Feature.Filtering
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
				return filter.FilterCondition switch
				{
					FilterCondition.GreaterThan => Expression.GreaterThan(propertyExpression, Expression.Constant((DateTime)filter.SearchValue)),
					FilterCondition.GreaterThanOrEqual => Expression.GreaterThanOrEqual(propertyExpression, Expression.Constant((DateTime)filter.SearchValue)),
					FilterCondition.LessThan => Expression.LessThan(propertyExpression, Expression.Constant((DateTime)filter.SearchValue)),
					FilterCondition.LessThanOrEqual => Expression.LessThanOrEqual(propertyExpression, Expression.Constant((DateTime)filter.SearchValue)),
					FilterCondition.Equals => Expression.Equal(propertyExpression, Expression.Constant((DateTime)filter.SearchValue)),
					FilterCondition.NotEquals => Expression.Not(Expression.Equal(propertyExpression, Expression.Constant((DateTime)filter.SearchValue))),
					FilterCondition.IsEmpty when isNullableType => Expression.Equal(propertyExpression, Expression.Constant(null, typeof(DateTime?))),
					FilterCondition.IsNotEmpty when isNullableType => Expression.Not(Expression.Equal(propertyExpression, Expression.Constant(null, typeof(DateTime?)))),
					FilterCondition.IsEmpty => throw new FilterConditionAndValueNotSupportedException($"Non-nullable DateTime filtering is not supported for {nameof(FilterCondition.IsEmpty)} condition."),
					FilterCondition.IsNotEmpty => throw new FilterConditionAndValueNotSupportedException($"Non-nullable DateTime filtering is not supported for {nameof(FilterCondition.IsNotEmpty)} condition."),
					_ => throw new FilterNotSupportedException(filter.FilterCondition, propertyExpression.Type)
				};
			}
			else if (propertyType == typeof(DateOnly) && !isInOrNotInCondition)
			{
				return filter.FilterCondition switch
				{
					FilterCondition.GreaterThan => Expression.GreaterThan(propertyExpression, Expression.Constant((DateOnly)filter.SearchValue)),
					FilterCondition.GreaterThanOrEqual => Expression.GreaterThanOrEqual(propertyExpression, Expression.Constant((DateOnly)filter.SearchValue)),
					FilterCondition.LessThan => Expression.LessThan(propertyExpression, Expression.Constant((DateOnly)filter.SearchValue)),
					FilterCondition.LessThanOrEqual => Expression.LessThanOrEqual(propertyExpression, Expression.Constant((DateOnly)filter.SearchValue)),
					FilterCondition.Equals => Expression.Equal(propertyExpression, Expression.Constant((DateOnly)filter.SearchValue)),
					FilterCondition.NotEquals => Expression.Not(Expression.Equal(propertyExpression, Expression.Constant((DateOnly)filter.SearchValue))),
					FilterCondition.IsEmpty when isNullableType => Expression.Equal(propertyExpression, Expression.Constant(null, typeof(DateOnly?))),
					FilterCondition.IsNotEmpty when isNullableType => Expression.Not(Expression.Equal(propertyExpression, Expression.Constant(null, typeof(DateOnly?)))),
					FilterCondition.IsEmpty => throw new FilterConditionAndValueNotSupportedException($"Non-nullable DateOnly filtering is not supported for {nameof(FilterCondition.IsEmpty)} condition."),
					FilterCondition.IsNotEmpty => throw new FilterConditionAndValueNotSupportedException($"Non-nullable DateOnly filtering is not supported for {nameof(FilterCondition.IsNotEmpty)} condition."),
					_ => throw new FilterNotSupportedException(filter.FilterCondition, propertyExpression.Type)
				};
			}
			else if (propertyType == typeof(TimeOnly) && !isInOrNotInCondition)
			{
				return filter.FilterCondition switch
				{
					FilterCondition.GreaterThan => Expression.GreaterThan(propertyExpression, Expression.Constant((TimeOnly)filter.SearchValue)),
					FilterCondition.GreaterThanOrEqual => Expression.GreaterThanOrEqual(propertyExpression, Expression.Constant((TimeOnly)filter.SearchValue)),
					FilterCondition.LessThan => Expression.LessThan(propertyExpression, Expression.Constant((TimeOnly)filter.SearchValue)),
					FilterCondition.LessThanOrEqual => Expression.LessThanOrEqual(propertyExpression, Expression.Constant((TimeOnly)filter.SearchValue)),
					FilterCondition.Equals => Expression.Equal(propertyExpression, Expression.Constant((TimeOnly)filter.SearchValue)),
					FilterCondition.NotEquals => Expression.Not(Expression.Equal(propertyExpression, Expression.Constant((TimeOnly)filter.SearchValue))),
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
					FilterCondition.In => CreateInExpression(propertyExpression, propertyType, valuesList),
					FilterCondition.NotIn => Expression.Not(CreateInExpression(propertyExpression, propertyType, valuesList)),
					_ => throw new FilterNotSupportedException(filter.FilterCondition, propertyExpression.Type)
				};
			}
			else if (ExpressionUtils.TryGetEnumerableElementType(propertyType, out var elementType))
			{
				return filter.FilterCondition switch
				{
					FilterCondition.Contains when elementType == typeof(string) => CreateEnumerableContainsExpression(propertyExpression, (string)filter.SearchValue!),
					FilterCondition.DoesNotContain when elementType == typeof(string) => Expression.Not(CreateEnumerableContainsExpression(propertyExpression, (string)filter.SearchValue!)),
					FilterCondition.ContainsAny => CreateContainsAnyExpression(propertyExpression, elementType, (List<object>)filter.SearchValue!),
					FilterCondition.DoesNotContainAny => Expression.Not(CreateContainsAnyExpression(propertyExpression, elementType, (List<object>)filter.SearchValue!)),
					FilterCondition.ContainsAll => CreateContainsAllExpression(propertyExpression, elementType, (List<object>)filter.SearchValue!),
					FilterCondition.DoesNotContainAll => Expression.Not(CreateContainsAllExpression(propertyExpression, elementType, (List<object>)filter.SearchValue!)),
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

		private static MethodCallExpression CreateContainsAnyExpression(Expression propertyExpression, Type? elementType, List<object> valuesList)
		{
			ArgumentNullException.ThrowIfNull(elementType, nameof(elementType));
			ArgumentNullException.ThrowIfNull(valuesList, nameof(valuesList));
			if (valuesList.Count > 0)
			{
				throw new ArgumentException("Values List must contain at least one value", nameof(valuesList));
			}

			BinaryExpression? combinedExpression = null;
			ParameterExpression parameter = Expression.Parameter(elementType, "y");

			//TODO: Is this the right implementation? look at the CreateContainsAllExpression for reference
			foreach (var value in valuesList)
			{
				var constantValue = value;

				if (combinedExpression == null)
				{
					combinedExpression = Expression.Equal(
						Expression.Equal(parameter, Expression.Constant(constantValue, elementType)),
						Expression.Constant(true));
				}
				else
				{
					combinedExpression = Expression.OrElse(
						combinedExpression,
						Expression.Equal(parameter, Expression.Constant(constantValue, elementType)));
				}
			}

			var anyMethod = AnyMethodInfo.MakeGenericMethod(elementType);
			var lambda = Expression.Lambda(combinedExpression!, parameter);
			return Expression.Call(anyMethod, propertyExpression, lambda);
		}

		private static MethodCallExpression CreateContainsAllExpression(Expression propertyExpression, Type? elementType, List<object> valuesList)
		{
			ArgumentNullException.ThrowIfNull(elementType, nameof(elementType));
			ArgumentNullException.ThrowIfNull(valuesList, nameof(valuesList));
			if (!valuesList.Any())
			{
				throw new ArgumentException("Values List must contain at least one value", nameof(valuesList));
			}

			ParameterExpression parameter = Expression.Parameter(elementType, "y");
			ParameterExpression parameter2 = Expression.Parameter(elementType, "z");

			IList list = elementType switch
			{
				Type t when t == typeof(bool) => valuesList.Cast<bool>().ToList(),
				Type t when t == typeof(byte) => valuesList.Cast<byte>().ToList(),
				Type t when t == typeof(short) => valuesList.Cast<short>().ToList(),
				Type t when t == typeof(int) => valuesList.Cast<int>().ToList(),
				Type t when t == typeof(long) => valuesList.Cast<long>().ToList(),
				Type t when t == typeof(float) => valuesList.Cast<float>().ToList(),
				Type t when t == typeof(double) => valuesList.Cast<double>().ToList(),
				Type t when t == typeof(decimal) => valuesList.Cast<decimal>().ToList(),
				Type t when t == typeof(string) => valuesList.Cast<string>().ToList(),
				Type t when t == typeof(Guid) => valuesList.Cast<Guid>().ToList(),
				Type t when t == typeof(DateTime) => valuesList.Cast<DateTime>().ToList(),
				Type t when t == typeof(DateOnly) => valuesList.Cast<DateOnly>().ToList(),
				Type t when t == typeof(TimeOnly) => valuesList.Cast<TimeOnly>().ToList(),
				Type t when t == typeof(TimeSpan) => valuesList.Cast<TimeSpan>().ToList(),
				Type t when t == typeof(DateTimeOffset) => valuesList.Cast<DateTimeOffset>().ToList(),
				Type t when t.IsEnum => (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(t))!,
				_ => throw new NotImplementedException(),
			};

			if (elementType.IsEnum)
			{
				foreach (var value in valuesList)
				{
					if (Enum.TryParse(elementType, value?.ToString()!, out var enumValue))
					{
						list.Add(enumValue);
					}
					else
					{
						throw new Exception($"Invalid value ({value ?? "null"}) provided for type {elementType.Name}");
					}
				}
			}

			var allMethod = AllMethodInfo.MakeGenericMethod(elementType);
			var anyMethod = AnyMethodInfo.MakeGenericMethod(elementType);

			var anyLambda = Expression.Lambda(Expression.Equal(parameter, parameter2), parameter2);
			var anyMethodCall = Expression.Call(anyMethod, propertyExpression, anyLambda);

			var allLambda = Expression.Lambda(anyMethodCall, parameter);
			var allMethodCall = Expression.Call(allMethod, Expression.Constant(list), allLambda);

			return allMethodCall;
		}

		private static MethodCallExpression CreateInExpression(Expression propertyExpression, Type elementType, List<object> valuesList)
		{
			ArgumentNullException.ThrowIfNull(elementType, nameof(elementType));
			ArgumentNullException.ThrowIfNull(valuesList, nameof(valuesList));
			if (valuesList.Count == 0)
			{
				throw new ArgumentException("Values List must contain at least one value", nameof(valuesList));
			}

			IList list = elementType switch
			{
				Type t when t == typeof(bool) => valuesList.Cast<bool>().ToList(),
				Type t when t == typeof(byte) => valuesList.Cast<byte>().ToList(),
				Type t when t == typeof(short) => valuesList.Cast<short>().ToList(),
				Type t when t == typeof(int) => valuesList.Cast<int>().ToList(),
				Type t when t == typeof(long) => valuesList.Cast<long>().ToList(),
				Type t when t == typeof(float) => valuesList.Cast<float>().ToList(),
				Type t when t == typeof(double) => valuesList.Cast<double>().ToList(),
				Type t when t == typeof(decimal) => valuesList.Cast<decimal>().ToList(),
				Type t when t == typeof(string) => valuesList.Cast<string>().ToList(),
				Type t when t == typeof(Guid) => valuesList.Cast<Guid>().ToList(),
				Type t when t == typeof(DateTime) => valuesList.Cast<DateTime>().ToList(),
				Type t when t == typeof(DateOnly) => valuesList.Cast<DateOnly>().ToList(),
				Type t when t == typeof(TimeOnly) => valuesList.Cast<TimeOnly>().ToList(),
				Type t when t == typeof(TimeSpan) => valuesList.Cast<TimeSpan>().ToList(),
				Type t when t == typeof(DateTimeOffset) => valuesList.Cast<DateTimeOffset>().ToList(),
				Type t when t.IsEnum => (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(t))!,
				_ => throw new NotImplementedException(),
			};

			if (elementType.IsEnum)
			{
				foreach (var value in valuesList)
				{
					if (Enum.TryParse(elementType, value?.ToString()!, out var enumValue))
					{
						list.Add(enumValue);
					}
					else
					{
						throw new Exception($"Invalid value ({value ?? "null"}) provided for type {elementType.Name}");
					}
				}
			}

			var containsMethod = typeof(Enumerable).GetMethods()
				.Where(m => m.Name == METHOD_NAME_CONTAINS && m.GetParameters().Length == 2)
				.First()
				.MakeGenericMethod(elementType);
			var containsMethodCall = Expression.Call(containsMethod, Expression.Constant(list), propertyExpression);

			return containsMethodCall;
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

			var lambda = Expression.Lambda<Func<T, bool>>(combinedExpression!, parameterExpressio);

			return query.Where(lambda);
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
