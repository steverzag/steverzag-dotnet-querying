namespace Steverzag.QueryShaper.Pagination
{
	internal static class PaginationGuard
	{
		public static void ValidatePage(int pageIndex, int pageSize)
		{
			ArgumentOutOfRangeException.ThrowIfLessThan(pageIndex, 1);
			ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
		}

		public static bool TryGetSkip(int pageIndex, int pageSize, out int skip)
		{
			long offset = (pageIndex - 1L) * pageSize;
			if (offset > int.MaxValue)
			{
				skip = 0;
				return false;
			}

			skip = (int)offset;
			return true;
		}
	}
}
