using ExpDataWorker.PropertySelector;
using ExpDataWorkerBeta.Feature.Utils;

namespace ExpDataWorkerBeta.Feature.Filtering
{
	public record FilterNode
	{
		public PropertySelector FieldSelector { get; set; } = null!;
		public ValueResult2? SearchValue { get; set; }
		public LogicalConnector FilterLogicOption { get; set; }
		public FilterCondition FilterCondition { get; set; }
		public IEnumerable<FilterNode>? Filters { get; set; }
	}
}
