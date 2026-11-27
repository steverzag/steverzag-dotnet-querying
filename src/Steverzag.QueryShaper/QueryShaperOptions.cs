using Steverzag.PropertySelectors;

namespace Steverzag.QueryShaper
{
	/// <summary>
	/// The settings and guard rails <see cref="IQueryShaper"/> applies to client requests. Set them through
	/// <c>AddQueryShaper(o => …)</c> or the standard options pattern.
	/// </summary>
	/// <remarks>Invalid options, such as a cap below 1 or a default page size above the maximum, fail when <see cref="IQueryShaper"/> is first resolved.</remarks>
	public sealed class QueryShaperOptions
	{
		/// <summary>The page size for a request that doesn't give one. 10 by default.</summary>
		public int DefaultPageSize { get; set; } = 10;

		/// <summary>The largest page size a request may ask for. A larger size is rejected, not clamped. 100 by default.</summary>
		public int MaxPageSize { get; set; } = 100;

		/// <summary>The most filter nodes a request may have, groups included, so it also bounds how deeply groups nest. 50 by default.</summary>
		public int MaxFilters { get; set; } = 50;

		/// <summary>The most values in one list condition (<c>In</c>, <c>NotIn</c>, <c>ContainsAny</c>, <c>ContainsAll</c>). 100 by default.</summary>
		public int MaxListValues { get; set; } = 100;

		/// <summary>The most orders a request may have. 10 by default.</summary>
		public int MaxOrders { get; set; } = 10;

		/// <summary>The longest search a request may have, in characters, white space included. 200 by default.</summary>
		public int MaxSearchLength { get; set; } = 200;

		/// <summary>
		/// The members a request's filters and orders may reach. A field that reaches a hidden member is rejected as if
		/// the member didn't exist. <see cref="MemberFilter.All"/> by default; consider <see cref="MemberFilter.Portable"/>
		/// or a provider preset from the EF Core add-on.
		/// </summary>
		public MemberFilter MemberFilter { get; set; } = MemberFilter.All;

		/// <summary>
		/// Types the database can't sort, such as <see cref="decimal"/> on SQLite. An order on one is rejected as a request
		/// error instead of failing when the query runs. Empty by default; the EF Core add-on has a list per provider.
		/// </summary>
		public ISet<Type> UnsortableTypes { get; set; } = new HashSet<Type>();
	}
}
