using Microsoft.EntityFrameworkCore;

namespace Steverzag.QueryShaper.Tests.Infrastructure
{
	public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
	{
		public DbSet<Product> Products => Set<Product>();
		public DbSet<Category> Categories => Set<Category>();
		public DbSet<Review> Reviews => Set<Review>();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Product>(product =>
			{
				product.Property(p => p.Id).ValueGeneratedNever();
				product.HasOne(p => p.Category).WithMany().HasForeignKey(p => p.CategoryId);
				product.HasMany(p => p.Reviews).WithOne().HasForeignKey(r => r.ProductId);
			});

			modelBuilder.Entity<Category>().Property(c => c.Id).ValueGeneratedNever();
			modelBuilder.Entity<Review>().Property(r => r.Id).ValueGeneratedNever();
		}
	}
}
