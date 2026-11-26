using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Steverzag.PropertySelectors.ArithmeticSelectors
{
	internal class ArithmeticUtils
	{
		public static bool IsNumericType(Type type)
		{
			if (type == null)
				return false;

			type = Nullable.GetUnderlyingType(type) ?? type;

			return Type.GetTypeCode(type) switch
			{
				TypeCode.Byte or TypeCode.SByte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 or TypeCode.Decimal or TypeCode.Double or TypeCode.Single => true,
				_ => false,
			};
		}

		static readonly Dictionary<Type, int> _order = new()
			{
				{ typeof(byte), 1 },
				{ typeof(sbyte), 1 },

				{ typeof(short), 2 },
				{ typeof(ushort), 2 },

				{ typeof(int), 3 },
				{ typeof(uint), 3 },

				{ typeof(long), 4 },
				{ typeof(ulong), 4 },

				{ typeof(float), 5 },
				{ typeof(double), 6 },
				{ typeof(decimal), 7 }
			};

		private static Type GetWiderNumericType(Type a, Type b)
		{
			if (a == null || b == null)
				throw new ArgumentNullException();

			var aUnderlying = Nullable.GetUnderlyingType(a) ?? a;
			var bUnderlying = Nullable.GetUnderlyingType(b) ?? b;


			if (!_order.ContainsKey(aUnderlying) || !_order.ContainsKey(bUnderlying))
				throw new ArgumentException("Both types must be numeric");

			var wider = _order[aUnderlying] >= _order[bUnderlying] ? aUnderlying : bUnderlying;

			if (Nullable.GetUnderlyingType(a) != null || Nullable.GetUnderlyingType(b) != null)
				return typeof(Nullable<>).MakeGenericType(wider);

			return wider;
		}

		public static Type? DeclaredType(IArithmeticOperationSelector operation)
		{
			// Read by the base constructor too, before the operands are set.
			var left = operation.LeftExpression?.Type;

			if (operation.Operator == ArithmeticOperator.Negation)
				return left is not null && IsNumericType(left) ? left : null;

			var right = operation.RightExpression?.Type;

			return left is not null && right is not null && IsNumericType(left) && IsNumericType(right)
				? GetWiderNumericType(left, right)
				: null;
		}

		internal static (PropertySelector Operand, Type Type)? FindNonNumericOperand(PropertySelector left, Type? leftType, PropertySelector? right, Type? rightType)
		{
			if (leftType is not null && !IsNumericType(leftType))
				return (left, leftType);

			if (right is not null && rightType is not null && !IsNumericType(rightType))
				return (right, rightType);

			return null;
		}

		internal static string NonNumericDetail(ArithmeticOperator op, PropertySelector operand, Type type)
		{
			var typeName = Nullable.GetUnderlyingType(type) is { } underlying ? underlying.Name + "?" : type.Name;

			return op == ArithmeticOperator.Negation
				? $"'-' needs a numeric operand: '{operand}' is {typeName}."
				: $"'{ArithmeticOperationFormatter.Symbol(op)}' needs numeric operands: '{operand}' is {typeName}.";
		}

		internal static void RequireNumericOperands(IArithmeticOperationSelector operation, Expression left, Expression? right)
		{
			if (FindNonNumericOperand(operation.LeftExpression, left.Type, operation.RightExpression, right?.Type) is not { } bad)
				return;

			throw new SelectorParseException(
				NonNumericDetail(operation.Operator, bad.Operand, bad.Type),
				ArithmeticOperationFormatter.Format(operation),
				ArithmeticOperationFormatter.OperatorPosition(operation),
				operation.Operator == ArithmeticOperator.Negation ? "-" : ArithmeticOperationFormatter.Symbol(operation.Operator).ToString());
		}

		public static Type GetWiderNumericType(params List<Type?> types)
		{
			if (types == null || types.Count == 0)
				throw new ArgumentException("Types list cannot be null or empty");
			Type? wider = null;
			foreach (var type in types)
			{
				if (type == null)
					continue;
				if (!IsNumericType(type))
					throw new ArgumentException($"Type {type.Name} is not numeric");
				if (wider == null)
				{
					wider = type;
				}
				else
				{
					wider = GetWiderNumericType(wider, type);
				}
			}
			if (wider == null)
				throw new ArgumentException("No valid numeric types found in the list");
			return wider;
		}
	}
}
