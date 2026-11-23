
namespace Steverzag.QueryShaper.Filtering
{
	public class FilterParameters
	{
		public IEnumerable<string>? SearchLookups { get; set; }
		public string? SearchValue { get; set; }
		public IEnumerable<FilterNode>? Filters { get; set; }
	}
}
