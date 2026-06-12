using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;

namespace ExpDataWorker.PropertySelectors.ArithmeticSelectors
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

		public static Expression CreateUnaryMathFunction(this Expression? target, MethodInfo methodInfo, PropertySelector parameter)
		{
			var parameterExpr = ExpressionUtils.CreateMemberAccess(target, parameter);

			if (!ArithmeticUtils.IsNumericType(parameterExpr.Type))
			{
				throw new InvalidOperationException($"Parameters must be of numeric type for {methodInfo.Name} arithmetic function.");
			}

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

		public static Expression CreateBinaryMathFunction(this Expression? target, MethodInfo methodInfo, PropertySelector param1, PropertySelector param2)
		{
			var paramExpr1 = ExpressionUtils.CreateMemberAccess(target, param1);
			var paramExpr2 = ExpressionUtils.CreateMemberAccess(target, param2);

			if (!ArithmeticUtils.IsNumericType(paramExpr1.Type) || !ArithmeticUtils.IsNumericType(paramExpr2.Type))
			{
				throw new InvalidOperationException($"Both parameters must be of numeric types for {methodInfo.Name} arithmetic function.");
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
