using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorkerBeta.Feature.PropertySelectors
{
	public record FunctionPropertySelectorOverload(SortedList<int, Type> Parameters, Func<Expression, Expression> Behavior)
	{
	}
}
