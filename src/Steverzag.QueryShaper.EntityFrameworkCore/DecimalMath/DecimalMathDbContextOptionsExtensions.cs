using Microsoft.EntityFrameworkCore;

namespace Steverzag.QueryShaper.EntityFrameworkCore
{
	public static class DecimalMathDbContextOptionsExtensions
	{
		public static DbContextOptionsBuilder UseDecimalMathAsDouble(this DbContextOptionsBuilder optionsBuilder)
		{
			ArgumentNullException.ThrowIfNull(optionsBuilder);
			return optionsBuilder.AddInterceptors(DecimalMathAsDoubleInterceptor.Instance);
		}

		public static DbContextOptionsBuilder<TContext> UseDecimalMathAsDouble<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder)
			where TContext : DbContext
		{
			ArgumentNullException.ThrowIfNull(optionsBuilder);
			return optionsBuilder.AddInterceptors(DecimalMathAsDoubleInterceptor.Instance);
		}
	}
}
