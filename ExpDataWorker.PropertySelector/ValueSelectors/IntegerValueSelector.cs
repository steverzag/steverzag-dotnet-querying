using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelector.ValueSelectors
{
	internal record IntegerValueSelector : PropertySelector
	{
		public int Value { get; set; }

		public IntegerValueSelector(int value) : base(SelectorDefaults.IntegerValueSpecifier)
		{
			Value = value;

			PropertyAccess = (_) =>
			{
				return Implementations.CreateIntConstantAccesor(value);
			};
		}
	}
}
