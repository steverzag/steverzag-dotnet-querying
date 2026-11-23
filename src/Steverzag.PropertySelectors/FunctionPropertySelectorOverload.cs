using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Steverzag.PropertySelectors
{
	public record FunctionPropertySelectorOverload(SortedList<int, Type> Parameters, Func<Expression, Expression> Behavior)
	{
	}
}
