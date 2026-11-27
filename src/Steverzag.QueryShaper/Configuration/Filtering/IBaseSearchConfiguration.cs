using System.Linq.Expressions;

namespace Steverzag.QueryShaper.Configuration.Filtering
{
	/// <summary>
	/// Gives the base search configured for a type. Inject the one <c>AddQueryShaper</c> registers, and pass it to
	/// <c>ApplyBaseFilter</c>.
	/// </summary>
	public interface IBaseSearchConfiguration
	{
		/// <summary>Whether a search is configured for <typeparamref name="T"/>.</summary>
		/// <typeparam name="T">The type to search.</typeparam>
		/// <returns><see langword="true"/> if one is configured.</returns>
		bool HasConfiguration<T>();

		/// <summary>Returns the search configured for <typeparamref name="T"/>, for one search text, ready to pass to <c>Where</c>.</summary>
		/// <typeparam name="T">The type to search.</typeparam>
		/// <param name="searchValue">The text to search for. It reaches the database as a parameter.</param>
		/// <returns>The predicate.</returns>
		/// <exception cref="InvalidOperationException">No search is configured for <typeparamref name="T"/>.</exception>
		Expression<Func<T, bool>> GetSearchPredicate<T>(string searchValue);

		/// <summary>Returns the search configured for <typeparamref name="T"/>, for one search text, if there is one.</summary>
		/// <typeparam name="T">The type to search.</typeparam>
		/// <param name="searchValue">The text to search for. It reaches the database as a parameter.</param>
		/// <param name="predicate">The predicate, or <see langword="null"/> if no search is configured.</param>
		/// <returns><see langword="true"/> if a search is configured for <typeparamref name="T"/>.</returns>
		bool TryGetSearchPredicate<T>(string searchValue, out Expression<Func<T, bool>>? predicate);
	}
}
