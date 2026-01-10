using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpDataWorkerBeta.Feature.Pagination
{
	public static class PaginationExtensions
	{
		public static async Task<PaginatedList<T>> PaginateAsync<T>(this IQueryable<T> source, int pageIndex, int pageSize)
		{
			return await PaginatedList<T>.CreateAsync(source, pageIndex, pageSize);
		}
	}
}
