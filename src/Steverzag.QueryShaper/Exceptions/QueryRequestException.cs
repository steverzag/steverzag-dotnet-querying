namespace Steverzag.QueryShaper.Exceptions
{
	/// <summary>
	/// A client request is invalid. <see cref="IQueryShaper"/> reports every error in a request with this type, whatever
	/// the part, so a server can map it alone to a 400.
	/// </summary>
	/// <remarks>The message is <see cref="Path"/> and <see cref="Detail"/>, such as <c>filters[1].field: Property 'Pric' was not found on type 'Product'.</c></remarks>
	public sealed class QueryRequestException : Exception
	{
		/// <summary>Initializes the exception.</summary>
		/// <param name="path">Where the error is, such as <c>filters[1].field</c>, or an empty string for the whole input.</param>
		/// <param name="detail">What is wrong, without the path.</param>
		/// <param name="innerException">The original error, if any.</param>
		public QueryRequestException(string path, string detail, Exception? innerException = null)
			: base(path.Length == 0 ? detail : $"{path}: {detail}", innerException)
		{
			Path = path;
			Detail = detail;
		}

		/// <summary>
		/// Where the error is, relative to what the method received: <c>filters[1].filters[0].field</c> from
		/// <see cref="IQueryShaper.Apply{T}"/>, <c>[1].filters[0].field</c> from <see cref="IQueryShaper.ApplyFilters{T}"/>.
		/// </summary>
		public string Path { get; }

		/// <summary>What is wrong, without the path.</summary>
		public string Detail { get; }

		/// <summary>Returns the same error with its path placed under a parent, for code that passes part of a request on.</summary>
		/// <param name="parent">The parent's path, such as <c>page</c>.</param>
		/// <returns>A new exception: <c>size</c> under <c>page</c> becomes <c>page.size</c>, and <c>[0]</c> under <c>filters</c> becomes <c>filters[0]</c>.</returns>
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
