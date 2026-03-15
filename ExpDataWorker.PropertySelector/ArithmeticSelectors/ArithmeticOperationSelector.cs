using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelectors.ArithmeticSelectors
{
	public record ArithmeticOperationSelector : PropertySelector
	{
		internal List<PropertySelector> Operands { get; init; } = [];
		public ArithmeticOperationSelector(string operation) : base()
		{
			char[] separators = ['+', '-', '*', '/'];

			var expression = operation.Replace(" ", "");

			Operands = [.. expression.Split(separators).Select(x => (PropertySelector)x)];

			PlainValue = SelectorDefaults.ArithmeticOperation + ":" + expression;
		}
	}
}
