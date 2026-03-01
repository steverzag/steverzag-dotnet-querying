
using ExpDataWorker.PropertySelector;

namespace ExpDataWorkerBeta.Feature.Aggregating
{
	public record AggregateParameters
	{
		public List<PropertySelector> GroupBySelectors { get; set; } = [];
		public List<PropertySelector> AggregateSelectors { get; set; } = [];
		public int? NTop { get; set; }
	}
}
