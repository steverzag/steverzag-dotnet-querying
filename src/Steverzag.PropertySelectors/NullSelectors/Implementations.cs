using System.Linq.Expressions;
using Steverzag.PropertySelectors.ArithmeticSelectors;

namespace Steverzag.PropertySelectors.NullSelectors
{
	internal static class Implementations
	{
		public static Expression CreateCoalesceExpression(this Expression? target, IReadOnlyList<PropertySelector> fallbacks)
		{
			if (target is null or ParameterExpression)
				throw new InvalidOperationException(
					$"{SelectorDefaults.Coalesce} must be applied to a piped value, e.g. 'Value{SelectorDefaults.PropertyDelimiter}{SelectorDefaults.Coalesce}(0)'.");

			var argumentRoot = ExpressionUtils.FindRootParameter(target) ?? target;

			var operands = new Expression[fallbacks.Count + 1];
			operands[0] = target;
			for (var i = 0; i < fallbacks.Count; i++)
				operands[i + 1] = ExpressionUtils.CreateMemberAccess(argumentRoot, fallbacks[i]);

			var resultType = ResolveCoalesceType([.. operands.Select(o => o.Type)])!;
			var underlying = Nullable.GetUnderlyingType(resultType) ?? resultType;
			var nullableType = underlying.IsValueType ? typeof(Nullable<>).MakeGenericType(underlying) : underlying;

			Expression result = ConvertIfNeeded(operands[^1], resultType);
			for (var i = operands.Length - 2; i >= 0; i--)
				result = Expression.Coalesce(ConvertIfNeeded(operands[i], nullableType), result);

			return result;
		}

		public static Type? ResolveCoalesceType(List<Type?> operandTypes)
		{
			for (var i = 0; i < operandTypes.Count; i++)
			{
				var type = operandTypes[i];
				if (type is null)
					continue;

				if (ExpressionUtils.TryGetEnumerableElementType(type, out _))
					throw new InvalidOperationException(
						$"{SelectorDefaults.Coalesce} can't be applied to collections: {DescribeOperand(i)} is a {type.Name}.");

				if (i < operandTypes.Count - 1 && !CanBeNull(type))
					throw new InvalidOperationException(
						$"{SelectorDefaults.Coalesce}: {DescribeOperand(i)} ({type.Name}) can never be null, so the operands after it are never used.");
			}

			if (operandTypes.Any(t => t is null))
				return operandTypes[0] is { IsValueType: false } subject ? subject : null;

			var underlyingTypes = operandTypes.Select(t => Nullable.GetUnderlyingType(t!) ?? t!).ToList();

			Type unified;
			if (underlyingTypes.All(t => !t.IsEnum && ArithmeticUtils.IsNumericType(t)))
			{
				unified = ArithmeticUtils.GetWiderNumericType([.. underlyingTypes]);
			}
			else
			{
				unified = underlyingTypes[0];
				var mismatch = underlyingTypes.FindIndex(t => t != unified);
				if (mismatch >= 0)
					throw new InvalidOperationException(
						$"{SelectorDefaults.Coalesce}: {DescribeOperand(mismatch)} is a {underlyingTypes[mismatch].Name}, which doesn't match {DescribeOperand(0)} ({unified.Name}).");
			}

			var last = operandTypes[^1]!;
			return unified.IsValueType && Nullable.GetUnderlyingType(last) is not null
				? typeof(Nullable<>).MakeGenericType(unified)
				: unified;
		}

		private static bool CanBeNull(Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;

		private static string DescribeOperand(int index) => index == 0 ? "the piped value" : $"argument {index}";

		private static Expression ConvertIfNeeded(Expression expression, Type type)
			=> expression.Type == type ? expression : Expression.Convert(expression, type);
	}
}
