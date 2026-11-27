using Steverzag.PropertySelectors;

namespace Steverzag.QueryShaper
{
	public sealed class QueryShaperOptions
	{
		public int DefaultPageSize { get; set; } = 10;

		public int MaxPageSize { get; set; } = 100;

		public int MaxFilters { get; set; } = 50;

		public MemberFilter MemberFilter { get; set; } = MemberFilter.All;

		public ISet<Type> UnsortableTypes { get; set; } = new HashSet<Type>();
	}
}
