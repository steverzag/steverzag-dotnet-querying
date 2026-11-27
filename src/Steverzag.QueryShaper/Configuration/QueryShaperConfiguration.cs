namespace Steverzag.QueryShaper.Configuration
{
	/// <summary>
	/// The place to register QueryShaper's maps and searches. Derive from it, override <see cref="OnConfiguring"/>, and
	/// pass the class to <c>AddQueryShaper&lt;TConfiguration&gt;</c>.
	/// </summary>
	/// <example>
	/// <code>
	/// public class AppQueryShaperConfig : QueryShaperConfiguration
	/// {
	///     protected override void OnConfiguring(QueryShaperConfigurationBuilder b) => b
	///         .AddMap&lt;Person, PersonDto&gt;(m => m.ForMember(d => d.PhoneCount, "Phones|$FN_AGGR_COUNT"))
	///         .ConfigureSearch&lt;Person&gt;(["Name", "Email"]);
	/// }
	/// </code>
	/// </example>
	public abstract class QueryShaperConfiguration
	{
		/// <summary>Registers the maps and searches.</summary>
		/// <param name="builder">The builder to register them with.</param>
		protected abstract void OnConfiguring(QueryShaperConfigurationBuilder builder);

		internal void Apply(QueryShaperConfigurationBuilder builder) => OnConfiguring(builder);
	}
}
