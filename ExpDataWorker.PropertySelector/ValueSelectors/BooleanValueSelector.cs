using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorker.PropertySelector.ValueSelectors
{
	internal record BooleanValueSelector : PropertySelector
	{
		public bool Value { get; set; }

		public BooleanValueSelector(bool value) : base(SelectorDefaults.BooleanValueSpecifier)
		{
			Value = value;

			PropertyAccess = (_) =>
			{
				return Implementations.CreateBoolConstantAccesor(value);
			};
		}
	}
}
