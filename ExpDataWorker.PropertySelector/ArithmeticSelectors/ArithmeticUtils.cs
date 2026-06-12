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
