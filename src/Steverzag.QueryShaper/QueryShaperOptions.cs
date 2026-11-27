using Steverzag.PropertySelectors;

namespace Steverzag.QueryShaper
{
	public sealed class QueryShaperOptions
	{
		public int DefaultPageSize { get; set; } = 10;

		public int MaxPageSize { get; set; } = 100;

		public int MaxFilters { get; set; } = 50;

		public int MaxListValues { get; set; } = 100;

		public int MaxOrders { get; set; } = 10;

		public int MaxSearchLength { get; set; } = 200;

		public MemberFilter MemberFilter { get; set; } = MemberFilter.All;

		public ISet<Type> UnsortableTypes { get; set; } = new HashSet<Type>();
	}
}
