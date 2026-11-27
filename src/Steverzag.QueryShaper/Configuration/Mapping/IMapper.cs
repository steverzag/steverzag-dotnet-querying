namespace Steverzag.QueryShaper.Configuration.Mapping
{
	/// <summary>
	/// Projects a query onto another type with the maps registered through <c>AddMap</c>. Inject the one
	/// <c>AddQueryShaper</c> registers.
	/// </summary>
	public interface IMapper
	{
		/// <summary>Projects a query with the map registered from <typeparamref name="TIn"/> to <typeparamref name="TOut"/>.</summary>
		/// <typeparam name="TIn">The source type.</typeparam>
		/// <typeparam name="TOut">The destination type.</typeparam>
		/// <param name="source">The query.</param>
		/// <returns>The projected query, still translatable by the provider.</returns>
		/// <exception cref="InvalidOperationException">No map is registered for the pair.</exception>
		IQueryable<TOut> Map<TIn, TOut>(IQueryable<TIn> source);
	}
}
