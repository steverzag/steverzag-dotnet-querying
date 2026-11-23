using Steverzag.PropertySelectors;
using Steverzag.QueryShaper.Utils;

namespace Steverzag.QueryShaper.Filtering
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
