using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelectors.ArithmeticSelectors
{
	public record ArithmeticOperationSelector : PropertySelector
	{
		public PropertySelector LeftExpression { get; set; }
		public PropertySelector? RightExpression { get; set; }
		public ArithmeticOperator Operator { get; set; }

		public ArithmeticOperationSelector() : base(SelectorDefaults.ArithmeticOperation)
		{
			PropertyAccess = (target) => target.CreateArithmeticOperationExpression(this);
		}
	}
}
