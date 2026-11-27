namespace Steverzag.QueryShaper.Exceptions
{
	public sealed class QueryRequestException : Exception
	{
		public QueryRequestException(string path, string detail, Exception? innerException = null)
			: base(path.Length == 0 ? detail : $"{path}: {detail}", innerException)
		{
			Path = path;
			Detail = detail;
		}

		public string Path { get; }

		public string Detail { get; }

		public QueryRequestException Within(string parent)
			=> new(Join(parent, Path), Detail, InnerException);

		private static string Join(string parent, string child) => child switch
		{
			"" => parent,
			_ when child.StartsWith('[') => parent + child,
			_ => parent + "." + child,
		};
	}
}
