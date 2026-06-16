using System.Linq.Expressions;

namespace ExpDataWorkerBeta.Feature.Configuration.Filtering
{
	/// <summary>
	/// Runtime-facing contract for resolving the preconfigured base (global) search of an entity type.
	/// The registration overloads live on the concrete <see cref="BaseSearchConfiguration"/>.
	/// </summary>
	public interface IBaseSearchConfiguration
	{
		bool HasConfiguration<T>();

		Expression<Func<T, bool>> GetSearchPredicate<T>(string searchValue);

		bool TryGetSearchPredicate<T>(string searchValue, out Expression<Func<T, bool>>? predicate);
	}
}
