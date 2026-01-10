using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorkerBeta.Feature.Aggregating
{
	public class AggregateResult
	{
		public List<GroupedByProperty> GroupedByProperties { get; set; }
		public List<AggregateValue> AggregateValues { get; set; }

		public AggregateResult(List<AggregateValue> aggregateValues)
		{
			
			GroupedByProperties = new List<GroupedByProperty>();
			AggregateValues = aggregateValues;
		}

		public AggregateResult(List<GroupedByProperty> groupedByProperties, List<AggregateValue> aggregateValues)
		{
			GroupedByProperties = groupedByProperties;
			AggregateValues = aggregateValues;
		}
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

	public class AggregateValue(string AggregateFunction, object? Value)
	{
		public string AggregateFunction { get; set; } = AggregateFunction;
		public object? Value { get; set; } = Value;
	}
}
