namespace Steverzag.QueryShaper.Configuration
{
	/// <summary>
	/// Ambient registry of the configuration artifacts (an <c>IMapper</c> / <c>ISearchConfiguration</c>)
	/// so the static query extensions can resolve them when a caller does not pass one explicitly.
	/// Populated by <c>AddQueryShaper</c>, so no separate initialization call is required.
	/// </summary>
	public static class QueryShaperServices
	{
		private static readonly Dictionary<Type, object> _instances = [];

		/// <summary>Stores <paramref name="instance"/> under the <typeparamref name="TContract"/> contract.</summary>
		internal static void Register<TContract>(TContract instance) where TContract : class
			=> _instances[typeof(TContract)] = instance;

		/// <summary>Returns the registered <typeparamref name="T"/>, or <c>null</c> if none was configured.</summary>
		internal static T? GetServiceOrNull<T>() where T : class
			=> _instances.TryGetValue(typeof(T), out var instance) ? (T)instance : null;
	}
}
