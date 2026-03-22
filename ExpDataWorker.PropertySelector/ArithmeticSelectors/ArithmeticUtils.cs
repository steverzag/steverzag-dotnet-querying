using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelectors.ArithmeticSelectors
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

		public static Type GetWiderNumericType(Type a, Type b)
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
	}
}
