using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steverzag.PropertySelectors.Extensions
{
	public static class PropertySelectorExtensions
	{
		public static PropertySelector<T> ToGeneric<T>(this PropertySelector selector)
		{
			var strSelector = selector.ToString();
			return new PropertySelector<T>(strSelector);
		}
	}
}
