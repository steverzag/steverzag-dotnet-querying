namespace Steverzag.QueryShaper
{
	public sealed class QueryShaperOptions
	{
		public int DefaultPageSize { get; set; } = 10;

		public int MaxPageSize { get; set; } = 100;
	}
}
