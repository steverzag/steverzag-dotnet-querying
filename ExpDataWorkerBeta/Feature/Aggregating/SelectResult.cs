using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorkerBeta.Feature.Aggregating
{
	public class SelectResult(List<SelectValue> Values)
	{
		public List<SelectValue> Values { get; set; } = Values;

	}

	public class GroupedByProperty
	{
		public string PropertyName { get; set; }
		public object? PropertyValue { get; set; }
		public GroupedByProperty(string propertyName, object? propertyValue)
		{
			PropertyName = propertyName;
			PropertyValue = propertyValue;
		}
	}

	public class SelectValue(string Selector, object? Value)
	{
		public string Selector { get; set; } = Selector;
		public object? Value { get; set; } = Value;
	}
}
