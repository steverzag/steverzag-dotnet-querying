using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;
using Steverzag.PropertySelectors.SelectorImplementations;

namespace Steverzag.PropertySelectors.ArithmeticSelectors
{
	internal static class Implementations
	{

		public static Expression CreateArithmeticOperationExpression<T>(this Expression? target, ArithmeticOperationSelector<T> selector)
		{
			if (selector.Operator == ArithmeticOperator.Negation)
			{
				var operand = ExpressionUtils.CreateMemberAccess(target, selector.LeftExpression);
				return Expression.Negate(operand);
			}

			var left = ExpressionUtils.CreateMemberAccess(target, selector.LeftExpression);
			var right = ExpressionUtils.CreateMemberAccess(target, selector.RightExpression!);

			var widerType = ArithmeticUtils.GetWiderNumericType(left.Type, right.Type);

			if (left.Type != widerType)
			{
				left = Expression.Convert(left, widerType);
			}
			if (right.Type != widerType)
			{
				right = Expression.Convert(right, widerType);
			}

			return selector.Operator switch
			{
				ArithmeticOperator.Addition => Expression.Add(left, right),
				ArithmeticOperator.Subtraction => Expression.Subtract(left, right),
				ArithmeticOperator.Multiplication => Expression.Multiply(left, right),
				ArithmeticOperator.Division => Expression.Divide(left, right),
				_ => throw new NotSupportedException($"Operator {selector.Operator}")
			};
		}

		public static Expression CreateArithmeticOperationExpression(this Expression? target, ArithmeticOperationSelector selector)
		{
			if (selector.Operator == ArithmeticOperator.Negation)
			{
				var operand = ExpressionUtils.CreateMemberAccess(target, selector.LeftExpression);
				return Expression.Negate(operand);
			}

			var left = ExpressionUtils.CreateMemberAccess(target, selector.LeftExpression);
			var right = ExpressionUtils.CreateMemberAccess(target, selector.RightExpression!);

			var widerType = ArithmeticUtils.GetWiderNumericType(left.Type, right.Type);

			if (left.Type != widerType)
			{
				left = Expression.Convert(left, widerType);
			}
			if (right.Type != widerType)
			{
				right = Expression.Convert(right, widerType);
			}

			return selector.Operator switch
			{
				ArithmeticOperator.Addition => Expression.Add(left, right),
				ArithmeticOperator.Subtraction => Expression.Subtract(left, right),
				ArithmeticOperator.Multiplication => Expression.Multiply(left, right),
				ArithmeticOperator.Division => Expression.Divide(left, right),
				_ => throw new NotSupportedException($"Operator {selector.Operator}")
			};
		}

		private static readonly ConcurrentDictionary<(MethodInfo DoubleOverload, Type OperandType), MethodInfo> TypedMathMethods = new();

		private static readonly HashSet<MethodInfo> ExactDecimalOverloads =
		[
			MethodInfos.MathRoundMethodInfo,
			MethodInfos.MathRound_2_MethodInfo,
			MethodInfos.MathCeilingMethodInfo,
			MethodInfos.MathFloorMethodInfo,
			MethodInfos.MathTruncateMethodInfo,
		];

		public static MethodInfo ForOperandType(this MethodInfo doubleOverload, Type? operandType)
		{
			var underlying = operandType is null ? null : Nullable.GetUnderlyingType(operandType) ?? operandType;
			if (underlying is null || underlying == typeof(double)
				|| (underlying == typeof(decimal) && !ExactDecimalOverloads.Contains(doubleOverload)))
				return doubleOverload;

			return TypedMathMethods.GetOrAdd((doubleOverload, underlying), static key =>
			{
				var parameterTypes = key.DoubleOverload.GetParameters()
					.Select(p => p.ParameterType == typeof(double) ? key.OperandType : p.ParameterType)
					.ToArray();

				return typeof(Math).GetMethods().FirstOrDefault(m => m.Name == key.DoubleOverload.Name
						&& m.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameterTypes))
					?? key.DoubleOverload;
			});
		}

		public static Type? MathResultType(MethodInfo doubleOverload, IEnumerable<Type?> operandTypes)
		{
			var types = operandTypes.ToList();

			return types.Count == 0 || types.Any(t => t is null)
				? null
				: doubleOverload.ForOperandType(ArithmeticUtils.GetWiderNumericType(types)).ReturnType;
		}

		public static Type? MaxMinResultType(IEnumerable<Type?> operandTypes)
		{
			var types = operandTypes.ToList();
			if (types.Count == 0 || types.Any(t => t is null))
				return null;

			var wider = ArithmeticUtils.GetWiderNumericType(types);
			var common = Nullable.GetUnderlyingType(wider) ?? wider;

			return types.All(t => Nullable.GetUnderlyingType(t!) is not null)
				? typeof(Nullable<>).MakeGenericType(common)
				: common;
		}

		public static Expression CreateMaxOrMin(this Expression? target, PropertySelector param1, PropertySelector param2, bool max)
		{
			var a = ExpressionUtils.ResolveArgument(target, param1);
			var b = ExpressionUtils.ResolveArgument(target, param2);

			if (!ArithmeticUtils.IsNumericType(a.Type) || !ArithmeticUtils.IsNumericType(b.Type))
				throw new InvalidOperationException($"Both parameters must be of numeric types for {(max ? "Max" : "Min")} arithmetic function.");

			var wider = ArithmeticUtils.GetWiderNumericType([a.Type, b.Type]);
			var common = Nullable.GetUnderlyingType(wider) ?? wider;
			var nullableCommon = typeof(Nullable<>).MakeGenericType(common);

			var aNullable = Nullable.GetUnderlyingType(a.Type) is not null;
			var bNullable = Nullable.GetUnderlyingType(b.Type) is not null;

			a = ConvertTo(a, aNullable ? nullableCommon : common);
			b = ConvertTo(b, bNullable ? nullableCommon : common);

			var aValue = ConvertTo(a, common);
			var bValue = ConvertTo(b, common);

			var pick = Expression.Condition(
				max ? Expression.GreaterThanOrEqual(aValue, bValue) : Expression.LessThanOrEqual(aValue, bValue),
				aValue,
				bValue);

			return (aNullable, bNullable) switch
			{
				(true, true) => Expression.Condition(IsNull(a), b,
					Expression.Condition(IsNull(b), a, Expression.Convert(pick, nullableCommon))),
				(true, false) => Expression.Condition(IsNull(a), bValue, pick),
				(false, true) => Expression.Condition(IsNull(b), aValue, pick),
				_ => pick,
			};

			static Expression ConvertTo(Expression expression, Type type)
				=> expression.Type == type ? expression : Expression.Convert(expression, type);

			static Expression IsNull(Expression expression)
				=> Expression.Equal(expression, Expression.Constant(null, expression.Type));
		}

		public static Expression CreateUnaryMathFunction(this Expression? target, MethodInfo methodInfo, PropertySelector parameter, bool matchOperandType = false)
		{
			var parameterExpr = ExpressionUtils.ResolveArgument(target, parameter);

			if (!ArithmeticUtils.IsNumericType(parameterExpr.Type))
			{
				throw new InvalidOperationException($"Parameters must be of numeric type for {methodInfo.Name} arithmetic function.");
			}

			if (matchOperandType)
				methodInfo = methodInfo.ForOperandType(parameterExpr.Type);

			var parameters = methodInfo.GetParameters();

			if (parameters.Length != 1)
			{
				throw new InvalidOperationException($"Method {methodInfo.Name} must have exactly one parameters.");
			}

			var parameter1Type = parameters[0].ParameterType;

			if (parameterExpr.Type != parameter1Type)
			{
				parameterExpr = Expression.Convert(parameterExpr, parameter1Type);
			}

			return Expression.Call(methodInfo, parameterExpr);
		}

		public static Expression CreateBinaryMathFunction(this Expression? target, MethodInfo methodInfo, PropertySelector param1, PropertySelector param2, bool matchOperandType = false)
		{
			var paramExpr1 = ExpressionUtils.ResolveArgument(target, param1);
			var paramExpr2 = ExpressionUtils.ResolveArgument(target, param2);

			if (!ArithmeticUtils.IsNumericType(paramExpr1.Type) || !ArithmeticUtils.IsNumericType(paramExpr2.Type))
			{
				throw new InvalidOperationException($"Both parameters must be of numeric types for {methodInfo.Name} arithmetic function.");
			}

			if (matchOperandType)
			{
				var operandTypes = methodInfo.GetParameters()
					.Zip([paramExpr1.Type, paramExpr2.Type])
					.Where(pair => pair.First.ParameterType == typeof(double))
					.Select(pair => (Type?)pair.Second)
					.ToList();

				methodInfo = methodInfo.ForOperandType(ArithmeticUtils.GetWiderNumericType(operandTypes));
			}

			var parameters = methodInfo.GetParameters();

			if(parameters.Length != 2)
			{
				throw new InvalidOperationException($"Method {methodInfo.Name} must have exactly two parameters.");
			}

			var parameter1Type = parameters[0].ParameterType;
			var parameter2Type = parameters[1].ParameterType;

			if (paramExpr1.Type != parameter1Type)
			{
				paramExpr1 = Expression.Convert(paramExpr1, parameter1Type);
			}

			if (paramExpr2.Type != parameter2Type)
			{
				paramExpr2 = Expression.Convert(paramExpr2, parameter2Type);
			}

			return Expression.Call(methodInfo, paramExpr1, paramExpr2);
		}
	}
}
