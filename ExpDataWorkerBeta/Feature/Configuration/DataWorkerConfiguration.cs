namespace ExpDataWorkerBeta.Feature.Configuration
{
	/// <summary>
	/// Base class for a single, centralized configuration of the data worker. Extend it and override
	/// <see cref="OnConfiguring"/> to register object maps and global searches in one place, much like
	/// overriding <c>OnConfiguring</c> on an EF Core <c>DbContext</c>.
	/// </summary>
	public abstract class DataWorkerConfiguration
	{
		/// <summary>
		/// Override to register maps and configure global searches through the supplied
		/// <paramref name="builder"/>.
		/// </summary>
		protected abstract void OnConfiguring(DataWorkerConfigurationBuilder builder);

		/// <summary>
		/// Applies this configuration's maps and searches onto the supplied builder (which wraps the
		/// shared registry instances). Intended for use by the DI registration; not called directly.
		/// </summary>
		internal void Apply(DataWorkerConfigurationBuilder builder) => OnConfiguring(builder);
	}
}
