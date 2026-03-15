using ExpDataWorker.PropertySelectors.SelectorImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelectors.ArithmeticSelectors
{
	internal static class Implementations
	{

		internal static Expression CreatePowFunction(this Expression target, PropertySelector number, PropertySelector exponent)
		{
			var leftExpr = number.PropertyAccess(target);
			var rightExpr = exponent.PropertyAccess(target);

			if (leftExpr.Type != typeof(double))
			{
				leftExpr = Expression.Convert(leftExpr, typeof(double));
			}

			if (rightExpr.Type != typeof(double))
			{
				rightExpr = Expression.Convert(rightExpr, typeof(double));
			}

			return Expression.Power(leftExpr, rightExpr);
		}

		internal static Expression CreateCeilFunction(this Expression target, PropertySelector number)
		{
			var numberExpr = number.PropertyAccess(target);
			if (numberExpr.Type != typeof(double))
			{
				numberExpr = Expression.Convert(numberExpr, typeof(double));
			}
			var ceilMethodInfo = MethodInfos.CeilingMethodInfo;
			return Expression.Call(ceilMethodInfo, numberExpr);
		}

		internal static Expression CreateFloorFunction(this Expression target, PropertySelector number)
		{
			var numberExpr = number.PropertyAccess(target);
			if (numberExpr.Type != typeof(double))
			{
				numberExpr = Expression.Convert(numberExpr, typeof(double));
			}
			var floorMethodInfo = MethodInfos.FloorMethodInfo;
			return Expression.Call(floorMethodInfo, numberExpr);
		}

		internal static Expression CreateRoundFunction(this Expression target, PropertySelector number)
		{
			var numberExpr = number.PropertyAccess(target);
			if (numberExpr.Type != typeof(double))
			{
				numberExpr = Expression.Convert(numberExpr, typeof(double));
			}
			var roundMethodInfo = MethodInfos.RoundMethodInfo;
			return Expression.Call(roundMethodInfo, numberExpr);
		}

		internal static Expression CreateRoundFunction(this Expression target, PropertySelector number, PropertySelector precission)
		{
			var numberExpr = number.PropertyAccess(target);
			var precissionExpr = precission.PropertyAccess(target);
			if (numberExpr.Type != typeof(double))
			{
				numberExpr = Expression.Convert(numberExpr, typeof(double));
			}
			if (precissionExpr.Type != typeof(int))
			{
				precissionExpr = Expression.Convert(precissionExpr, typeof(int));
			}

			var roundMethodInfo = MethodInfos.Round_2_MethodInfo;
			return Expression.Call(roundMethodInfo, numberExpr, precissionExpr);
		}
	}
}
